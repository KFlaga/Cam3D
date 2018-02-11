#pragma once

#include "SgmCommon.hpp"
#include "SgmPathsManager.hpp"
#include "SgmPath.hpp"
#include "SgmDisparityComputer.hpp"
#include <stdexcept>
#include <atomic>
#include <mutex>

namespace cam3d
{
template<typename ImageT, typename CostComputerT>
class SgmCostAggregator : public ISgmCostAggregator
{
public:
    using Image = ImageT;
	using CostComputer = CostComputerT;
    using DisparityComputer = SgmDisparityComputer<Image, CostComputer>;

private:
    int rows;
    int cols;
    int maxDisparity;
    double lowPenaltyCoeff; // P1 = coeff * MaxCost
    double highPenaltyCoeff; // P2 = coeff * MaxCost
    double intensityThreshold;
    bool isLeftImageBase = true;
    std::vector<double> thisStepCosts;
    SgmPathsManager pathMgr;

    double P1;
    double P2;

	Image& imageBase;
	Image& imageMatched;
	DisparityMap& map;
    CostComputer costComp;
    DisparityComputer dispComp;

    Point2 currentPixel;
    Point2 matched;

	std::atomic_bool shouldTerminate;
	std::mutex statusMutex;
	std::function<std::string()> statusPrinter;

public:
    SgmCostAggregator(SgmParameters& params, bool isLeftBase, Image& imageBase_, Image& imageMatched_, DisparityMap& map_) :
        rows{ params.rows },
        cols{ params.cols },
        isLeftImageBase{ isLeftBase },
		imageBase{imageBase_},
		imageMatched{imageMatched_},
		map{map_},
        pathMgr{ params.rows, params.cols, [this](Point2 p1, Point2 p2){ return this->getCost(p1, p2); },
            [this](Point2 p){ return this->getDispRange(p.x); }, isLeftImageBase},
        costComp{ params.rows, params.cols },
		dispComp{ params.rows, params.cols, map_, imageBase_, imageMatched_, costComp},
		statusPrinter{ [this]() { return std::string{"Not run"}; } }
    {
		setParameters(params);
		shouldTerminate = false;
    }

    int getRows() const { return rows; }
    int getCols() const { return cols; }
	bool getIsLeftImageBase() const { return isLeftImageBase; }
	Image& getBaseImage() { return imageBase; }
	Image& getMatchedImage() { return imageMatched; }
	DisparityMap& getDisparityMap() { return map; }
	CostComputer& getCostComp() { return costComp; }
	DisparityComputer& getDispComp() { return dispComp; }
	void changeStatus(std::function<std::string()> f)
	{
		std::lock_guard<std::mutex> lock(statusMutex);
		statusPrinter = f;
	}

    void setParameters(SgmParameters& params);
	void terminate() override { shouldTerminate = true; }
	std::string getState() override 
	{
		std::lock_guard<std::mutex> lock(statusMutex);
		return statusPrinter(); 
	}

    void computeMatchingCosts() override
    {
		initLocalCosts();
		initPaths();
        findCostsTopDown();
        findCostsBottomUp();
        findDisparities();
		done();
    }

    int getDispRange(int pixelX)
    {
        return isLeftImageBase ?
                    std::min(pixelX - 1, maxDisparity) :
                    std::min(cols - 1 - pixelX, maxDisparity);
    }

	void initLocalCosts()
	{
		changeStatus([this]()
		{
			Point2 pixel = this->costComp.getCurrentPixel();
			return "Computing Census: {" + to_string(pixel) + "}";
		});
		costComp.init(imageBase, imageMatched);
		P1 = lowPenaltyCoeff * costComp.getMaxCost();
		P2 = highPenaltyCoeff * costComp.getMaxCost();
	}

	void initPaths()
	{
		changeStatus([this]() { return std::string{ "Preparing Paths" }; });
		pathMgr.init();
		thisStepCosts.resize(cols + 1);
	}

	void done()
	{
		changeStatus([this]() { return std::string{ "Done" }; });
	}

    void findCostsTopDown()
    {
		int y, x;
		changeStatus([this, &x, &y]() { return "Run: TopDown { " + to_string(Point2{ y, x }) + " }"; });
        for(y = 0; y < rows; ++y)
        {
            for(x = 0; x < cols; ++x)
            {
                if (shouldTerminate) { return; }
                findCostsForPixel(y, x, RunDirection::TopDown);
            }
        }
    }

    void findCostsBottomUp()
    {
		int y, x;
		changeStatus([this, &x, &y]() { return "Run: BottomUp { " + to_string(Point2{ y, x }) + " }"; });
        for(y = rows - 1; y >= 0; --y)
        {
            for(x = cols - 1; x >= 0; --x)
            {
                if (shouldTerminate) { return; }
                findCostsForPixel(y, x, RunDirection::BottomUp);
            }
        }
    }

    void findCostsForPixel(int y, int x, RunDirection dir)
    {
        int maxDisp = getDispRange(x);
        for(int pathIdx : pathMgr.getPathIdxsForRun(dir))
        {
            findCostsForPath({ y, x }, pathIdx, maxDisp, dir == RunDirection::BottomUp);
        }
    }

    void findCostsForPath(Point2 currentPixel, int pathIdx, int maxDisp, bool isBottomUp)
    {
		Point2 borderPixel = pathMgr.getBorderPixel(currentPixel, pathIdx);
        SgmPath* path = pathMgr.getPath(borderPixel, pathIdx);
        TEST_checkPathCorrectness(path, currentPixel);

        findCostForEachDisparityInStep(path, pathIdx, maxDisp);

        if(isBottomUp && maxDisp > 0)
        {
            alignForBottomUp(path, maxDisp, currentPixel);
        }

        path->next();
    }

    void findCostForEachDisparityInStep(SgmPath* path, int pathIdx, int maxDisp)
    {
        int bestDisp = 0;
        int bestLength = 0;
        double bestCost = 1e12;
        auto bestPrevCost = pathMgr.getBestPathCosts(path->previousPixel, pathIdx);

        for(int d = 0; d < maxDisp; ++d)
        {
			Point2 matched = { path->currentPixel.y,
				isLeftImageBase ? path->currentPixel.x - d : path->currentPixel.x + d 
			};

            double cost = findCostForDisparity(
                              path->currentPixel, matched, path, d, maxDisp, bestPrevCost.disparity, bestPrevCost.cost);
            thisStepCosts[d] = cost;

            if(bestCost > cost)
            {
                bestCost = cost;
                bestDisp = d;
                bestLength = path->currentIndex + 1;
            }
        }
        pathMgr.setBestPathCosts(path->currentPixel, pathIdx, {bestCost, bestDisp, bestLength});
        std::copy(thisStepCosts.begin(), thisStepCosts.end(), path->lastStepCosts.begin());
    }

    void alignForBottomUp(SgmPath* path, int maxDisp, Point2 currentPixel)
    {
        // For disparity greater than max, matched pixel may exceed image dimensions:
        // L[p, d > dmax-1] = Cost(curPix, maxXPix) + LastCost[dmax-1]
        // We actualy need only to compute L[p, dmax] as it will be needed in next iteration
        int matchedX = isLeftImageBase ? 0 : cols - 1;
        path->lastStepCosts[maxDisp] = // As LastStepCosts is of size dispRange + 1 we won't exceed max index
                getCost(currentPixel, { currentPixel.y, matchedX }) + path->lastStepCosts[maxDisp - 1];
    }

    void TEST_checkPathCorrectness(SgmPath* path, Point2 currentPixel)
    {
        if(path == nullptr)
        {
            throw std::logic_error(std::string("File: ") + __FILE__ + ", line: " + std::to_string(__LINE__));
        }
        if(path->length <= 0)
        {
            throw std::logic_error(std::string("File: ") + __FILE__ + ", line: " + std::to_string(__LINE__));
        }
        if(path->currentIndex >= path->length)
        {
            throw std::logic_error(std::string("File: ") + __FILE__ + ", line: " + std::to_string(__LINE__));
        }
		if (path->currentPixel != currentPixel)
		{
			throw std::logic_error(std::string("File: ") + __FILE__ + ", line: " + std::to_string(__LINE__));
		}
    }

    double findCostForDisparity(Point2 currentPixel, Point2 matched, SgmPath* path, int d, int dmax, int bestPrevDisp, double bestPrevCost)
    {
        TEST_checkPointsCorectness(currentPixel, matched);

        double pen0 = path->lastStepCosts[d];
        double pen1 = findPenaltyClose(path, d, dmax);
        double pen2 = findPenaltyFar(dmax, d, bestPrevCost, bestPrevDisp, path);

        double c = costComp.getCost(currentPixel, matched);
		double imgDiff = std::abs(imageBase(currentPixel.y, currentPixel.x) - imageMatched(matched.y, matched.x));
        return c + minFrom(
            pen0,
            pen1 + P1,
            pen2 + P2 * (imgDiff > intensityThreshold ? 1.0 : 2.0));
    }

    double findPenaltyClose(SgmPath* path, int d, int dmax)
    {
        return d == 0 ? path->lastStepCosts[d + 1] :
            d > dmax - 1 ? path->lastStepCosts[d - 1] :
            std::min(path->lastStepCosts[d + 1], path->lastStepCosts[d - 1]);
    }

    double findPenaltyFar(int dmax, int d, double bestPrevCost, int bestPrevDisp, SgmPath* path)
    {
		return bestPrevCost;
    }

    void TEST_checkPointsCorectness(Point2 basePixel, Point2 matched)
    {
        if(basePixel.x >= cols || matched.x >= cols || basePixel.x < 0 || matched.x < 0)
        {
            throw std::logic_error(std::string("File: ") + __FILE__ + ", line: " + std::to_string(__LINE__));
        }
    }

    double getCost(Point2 basePixel, Point2 matchedPixel)
    {
        return costComp.getCost(basePixel, matchedPixel);
    }

    void findDisparities()
    {
		int r, c;
		changeStatus([this, &r, &c]() { return "Run: Disparities { " + to_string(Point2{ r, c }) + " }"; });
        for(r = 0; r < rows; ++r)
        {
            for(c = 0; c < cols; ++c)
            {
				if (shouldTerminate)
					return;

                Point2 currentPixel = {r, c};
                for(int i = 0; i < pathsCount; ++i)
                {
                    auto best = pathMgr.getBestPathCosts(currentPixel, i);
                    int dx = best.disparity * (isLeftImageBase ? -1 : 1);
					double matchCost = getCost(currentPixel, { currentPixel.y, currentPixel.x + dx });
                    dispComp.storeDisparity(DisparityForPixel{
                        dx, best.pathLength, best.cost, matchCost
                    });
                }
                dispComp.finalizeForPixel(currentPixel);
            }
        }
    }
};

template<typename ImageT, typename CostComputerT>
void SgmCostAggregator<ImageT, CostComputerT>::setParameters(SgmParameters& params)
{
    this->maxDisparity = params.maxDisparity;
	this->lowPenaltyCoeff = params.lowPenaltyCoeff;
	this->highPenaltyCoeff = params.highPenaltyCoeff;
    this->intensityThreshold = params.intensityThreshold;
	this->costComp.setMaskWidth(params.censusMaskRadius);
	this->costComp.setMaskHeight(params.censusMaskRadius);
	this->dispComp.setCostMethod((cam3d::CostMethod)params.disparityCostMethod);
	this->dispComp.setMeanMethod((cam3d::MeanMethod)params.disparityMeanMethod);
    this->dispComp.setCostMethodPower(params.costMethodPower);
	this->dispComp.setPathLengthTreshold(params.diparityPathLengthThreshold);
}
}
