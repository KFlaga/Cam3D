#pragma once

#include "SgmCommon.hpp"
#include "SgmPath.hpp"

namespace cam3d
{
class SgmPathsManager
{
private:
	int rows;
	int cols;
    bool isLeftImageBase;
    Array3d<SgmPath*> paths;
    Array3d<PathCost> bestPathsCosts;
    using BorderPixelGetter = Point2(*)(Point2, int, int);
    BorderPixelGetter borderPixelGetters[pathsCount];
    std::function<double(Point2, Point2)> getCost;
    std::function<int(Point2)> getDispRange;
public:
    static constexpr int pathsPerRun = pathsCount / 2;

    SgmPathsManager(int rows, int cols, std::function<double(Point2, Point2)> getCost,
                    std::function<int(Point2)> getDispRange, bool isLeftImageBase);
    ~SgmPathsManager();

    void init();
    std::array<int, pathsPerRun> getPathIdxsForRun(RunDirection dir);

    SgmPath* getPath(Point2 pixel, int pathNum) const
    {
        return paths(pixel, pathNum);
    }

    PathCost getBestPathCosts(Point2 pixel, int pathNum) const
    {
        return bestPathsCosts(pixel, pathNum);
    }

    void setBestPathCosts(Point2 pixel, int pathNum, PathCost cost)
    {
        bestPathsCosts(pixel, pathNum) = cost;
    }

    Point2 getBorderPixel(Point2 point, int pathDir)
    {
        return borderPixelGetters[pathDir](point, rows, cols);
    }

private:
    void createBorderPaths();
    void createPathsForBorderPixel(Point2 pixel);
    void initZeroStep(Point2 borderPixel);
    void findInitialCostOnPath(SgmPath* path, int pathNum);
    void initBorderPixelGetters();
};
}
