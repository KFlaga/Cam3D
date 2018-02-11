#pragma once

#include "SgmCommon.hpp"
#include <CamCommon/GreyScaleImage.hpp>
#include "CensusCostComputer.hpp"

namespace cam3d
{
struct DisparityForPixel
{
    int disparity;
    int pathLength;
    double pathCost;
    double matchCost;
};

template<typename ImageT, typename CostComputerT>
class SgmDisparityComputer
{
protected:
    int rows;
    int cols;

	using Image = ImageT;
	using CostComputer = CostComputerT;
    using DisparityComputer = SgmDisparityComputer;
    using DisparityList = std::array<DisparityForPixel, pathsCount>;

    Image& imageBase;
    Image& imageMatched;
    DisparityMap& disparityMap;
    CostComputer& costComp;

    DisparityList dispForPixel;
    int disparityCount;
    double pathLengthTreshold;
    double costMethodPower;

    using ComputeMeanFunc = double(DisparityComputer::*)(int,int);
    ComputeMeanFunc computeMean;
    using ComputeCostFunc = double(DisparityComputer::*)(double,int,int);
    ComputeCostFunc computeCost;

    MeanMethod meanMethod;
    CostMethod costMethod;

public:
    SgmDisparityComputer(int rows_, int cols_, DisparityMap& map,
		Image& imageBase_, Image& imageMatched_, CostComputer& costComp_) :
        rows{rows_},
        cols{cols_},
        disparityCount{0},
        disparityMap{map},
        imageBase{imageBase_},
        imageMatched{imageMatched_},
        costComp{costComp_}
    {
        setMeanMethod(MeanMethod::SimpleAverage);
        setCostMethod(CostMethod::DistanceToMean);
        std::fill(dispForPixel.begin(), dispForPixel.end(), DisparityForPixel{});
    }

    MeanMethod getMeanMethod() const;
    void setMeanMethod(MeanMethod method);
    CostMethod getCostMethod() const;
    void setCostMethod(CostMethod method);
	double getPathLengthTreshold() const;
	void setPathLengthTreshold(double val);
    double getCostMethodPower() const;
    void setCostMethodPower(double val);

    DisparityMap& getDisparityMap() { return disparityMap; }

    void storeDisparity(DisparityForPixel disp)
    {
#ifdef _DEBUG
        if(disparityCount >= pathsCount)
        {
            throw std::logic_error(std::string("File: ") + __FILE__ + ", line: " + std::to_string(__LINE__));
        }
#endif
        dispForPixel[disparityCount] = disp;
        ++disparityCount;
    }

    void finalizeForPixel(Point2 pixelBase)
    {
        if(disparityCount == 0)
        {
#ifdef _DEBUG
            throw std::logic_error(std::string("File: ") + __FILE__ + ", line: " + std::to_string(__LINE__));
#endif
            // There was no disparity for pixel : set as invalid
            disparityMap(pixelBase) = Disparity{};
            return;
        }

        // 1) Sort by disparity
        std::sort(dispForPixel.begin(), dispForPixel.end(),
                  [](const auto d1, const auto d2){ return d1.disparity < d2.disparity; });
        disparityMap(pixelBase) = findBestDisparity(pixelBase);

        disparityCount = 0;
    }

protected:
    Disparity findBestDisparity(Point2 pixelBase)
    {
        int start = 0, count = disparityCount;
        double mean = (this->*computeMean)(start, count);
        double cost = (this->*computeCost)(mean, start, count);
        while(count > 2) // 4
        {
            double mean1 = (this->*computeMean)(start + 1, count - 1);
            double cost1 = (this->*computeCost)(mean1, start + 1, count - 1);
            double mean2 = (this->*computeMean)(start, count - 1);
            double cost2 = (this->*computeCost)(mean2, start, count - 1);

            if(cost > cost1 || cost > cost2)
            {
                if(cost1 < cost2)
                {
                    start++;
                    cost = cost1;
                    mean = mean1;
                }
                else
                {
                    cost = cost2;
                    mean = mean2;
                }
                count--;
            }
            else
            {
                break;
            }
        }
        cost = costComp.getCost(pixelBase, Point2{pixelBase.y, pixelBase.x + round(mean)});
        return Disparity{round(mean), Disparity::Valid, mean, cost, findConfidence(count, cost)};
    }

    double findConfidence(int count, double cost)
    {
        // TODO: Za współczynnik zaufania wyznaczenia dysparycji przyjęto stosunek końcowej i
        // początkowej sumy wag wk opisanych poniżej.
        return ((double)count / (double)disparityCount);
    }

    double findMean_Simple(int start, int count)
    {
        double mean = 0.0;
        for(int i = 0; i < count; ++i)
        {
            mean += dispForPixel[start + i].disparity;
        }
        return mean / count;
    }

    double findMean_WeightedPath(int start, int count)
    {
        double mean = 0.0, wsum = 0.0, w;
        for(int i = 0; i < count; ++i)
        {
            w = std::max(0.0, std::min(1.0, (dispForPixel[start + i].pathLength - pathLengthTreshold)/pathLengthTreshold));
            wsum += w;
            mean += w * dispForPixel[start + i].disparity;
        }
        return mean / wsum;
    }

    double findCost_Simple(double mean, int start, int count)
    {
        double cost = 0.0;
        // 1) C = sum(||m - d||) / n^2
        for(int i = 0; i < count; ++i)
        {
            cost += std::abs(mean - dispForPixel[start + i].disparity);
        }
        return cost / std::pow(count, costMethodPower * 0.5);
    }

    double findCost_Squared(double mean, int start, int count)
    {
        double cost = 0.0, d;
        for(int i = 0; i < count; ++i)
        {
            d = mean - dispForPixel[start + i].disparity;
            cost += d * d;
        }
        return cost / std::pow(count, costMethodPower);
    }
};

template<typename IT, typename CC>
MeanMethod SgmDisparityComputer<IT, CC>::getMeanMethod() const
{
    return meanMethod;
}

template<typename IT, typename CC>
void SgmDisparityComputer<IT, CC>::setMeanMethod(MeanMethod method)
{
    meanMethod = method;
    switch(method)
    {
    case MeanMethod::WeightedAverageWithPathLength:
        computeMean = &SgmDisparityComputer::findMean_WeightedPath;
        break;
    case MeanMethod::SimpleAverage:
    default:
        computeMean = &SgmDisparityComputer::findMean_Simple;
        break;
    }
}

template<typename IT, typename CC>
CostMethod SgmDisparityComputer<IT, CC>::getCostMethod() const
{
    return costMethod;
}

template<typename IT, typename CC>
void SgmDisparityComputer<IT, CC>::setCostMethod(CostMethod method)
{
    costMethod = method;
    switch(method)
    {
    case CostMethod::DistanceSquredToMean:
        computeCost = &SgmDisparityComputer::findCost_Squared;
        break;
    case CostMethod::DistanceToMean:
    default:
        computeCost = &SgmDisparityComputer::findCost_Simple;
        break;
    }
}

template<typename IT, typename CC>
double SgmDisparityComputer<IT, CC>::getPathLengthTreshold() const
{
	return pathLengthTreshold;
}

template<typename IT, typename CC>
void SgmDisparityComputer<IT, CC>::setPathLengthTreshold(double val)
{
	pathLengthTreshold = val;
}

template<typename IT, typename CC>
double SgmDisparityComputer<IT, CC>::getCostMethodPower() const
{
    return costMethodPower;
}

template<typename IT, typename CC>
void SgmDisparityComputer<IT, CC>::setCostMethodPower(double val)
{
    costMethodPower = val;
}

}
