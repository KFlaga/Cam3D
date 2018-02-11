#pragma once

#include "SgmPathsManager.hpp"
namespace cam3d
{
namespace
{
std::array<int, SgmPathsManager::pathsPerRun> pathsInRun_RightTopDown = {
    PathDirection::PosX,
    PathDirection::PosY,
    PathDirection::PosX_PosY,
    PathDirection::NegX_PosY
};

std::array<int, SgmPathsManager::pathsPerRun> pathsInRun_RightBottomUp = {
    PathDirection::NegX,
    PathDirection::NegY,
    PathDirection::PosX_NegY,
    PathDirection::NegX_NegY
};

std::array<int, SgmPathsManager::pathsPerRun> pathsInRun_LeftTopDown = {
    PathDirection::PosX,
    PathDirection::PosY,
    PathDirection::PosX_PosY,
    PathDirection::NegX_PosY
};

std::array<int, SgmPathsManager::pathsPerRun> pathsInRun_LeftBottomUp = {
    PathDirection::NegX,
    PathDirection::NegY,
    PathDirection::PosX_NegY,
    PathDirection::NegX_NegY
};
}

SgmPathsManager::SgmPathsManager(int rows_, int cols_, std::function<double(Point2, Point2)> getCost_,
                std::function<int(Point2)> getDispRange_, bool isLeftImageBase_) :
    rows{ rows_ },
    cols{ cols_ },
    isLeftImageBase(isLeftImageBase_),
    paths{rows_, cols_, pathsCount},
    bestPathsCosts{rows_, cols_, pathsCount},
    getCost{ getCost_ },
    getDispRange{getDispRange_}
{
}

SgmPathsManager::~SgmPathsManager()
{
    std::for_each(paths.begin(), paths.end(), [](SgmPath* p) { delete p; });
}

void SgmPathsManager::SgmPathsManager::init()
{
    createBorderPaths();
    initBorderPixelGetters();
}

std::array<int, SgmPathsManager::pathsPerRun> SgmPathsManager::getPathIdxsForRun(RunDirection dir)
{
    return dir == RunDirection::TopDown ?
        (isLeftImageBase ? pathsInRun_LeftTopDown : pathsInRun_RightTopDown) :
        (isLeftImageBase ? pathsInRun_LeftBottomUp : pathsInRun_RightBottomUp);
}


void SgmPathsManager::createBorderPaths()
{
    paths.fill(0);
    for(int x = 0; x < cols; ++x)
    {
        createPathsForBorderPixel({0, x});
        initZeroStep({0, x});

        createPathsForBorderPixel({rows - 1, x});
        initZeroStep({rows - 1, x});
    }

    for(int y = 1; y < rows; ++y)
    {
        createPathsForBorderPixel({y, 0});
        initZeroStep({y, 0});

        createPathsForBorderPixel({y, cols - 1});
        initZeroStep({y, cols - 1});
    }
}

void SgmPathsManager::createPathsForBorderPixel(Point2 pixel)
{
    // Create only those paths which can start on pixel (y,x)
    if(pixel.x == 0)
    {
        paths(pixel, PathDirection::PosX) = new SgmPath_PosX{};
    }
    if(pixel.x == cols - 1)
    {
        paths(pixel, PathDirection::NegX) = new SgmPath_NegX{};
    }
    if(pixel.y == 0)
    {
        paths(pixel, PathDirection::PosY) = new SgmPath_PosY{};
    }
    if(pixel.y == rows - 1)
    {
        paths(pixel, PathDirection::NegY) = new SgmPath_NegY{};
    }
    if(pixel.x == 0 || pixel.y == 0)
    {
        paths(pixel, PathDirection::PosX_PosY) = new SgmPath_PosX_PosY{};
    }
    if(pixel.x == cols - 1 || pixel.y == 0)
    {
        paths(pixel, PathDirection::NegX_PosY) = new SgmPath_NegX_PosY{};
    }
    if(pixel.x == 0 || pixel.y == rows - 1)
    {
        paths(pixel, PathDirection::PosX_NegY) = new SgmPath_PosX_NegY{};
    }
    if(pixel.x == cols - 1 || pixel.y == rows - 1)
    {
        paths(pixel, PathDirection::NegX_NegY) = new SgmPath_NegX_NegY{};
    }
}

void SgmPathsManager::initZeroStep(Point2 borderPixel)
{
    for(int i = 0; i < pathsCount; ++i)
    {
        SgmPath* path = paths(borderPixel, i);
        if(path != nullptr)
        {
            path->imageHeight = rows;
            path->imageWidth = cols;
            path->startPixel = borderPixel;
            path->length = rows + cols;
            path->lastStepCosts.resize(cols + 1); // Need cols + 1 for convinent bottom-up run
            path->init();

            findInitialCostOnPath(path, i);
        }
    }
}

void SgmPathsManager::findInitialCostOnPath(SgmPath* path, int pathNum)
{
    int bestDisp = 0;
    double bestCost = 1e12;
    int maxDisp = getDispRange(path->currentPixel);
    for(int d = 0; d < maxDisp; ++d)
    {
        double cost = getCost(path->currentPixel,
                              path->currentPixel + Point2{ 0, isLeftImageBase ? -d : d });
        path->lastStepCosts[d] = cost;

        if(bestCost > cost)
        {
            bestCost = cost;
            bestDisp = d;
        }
    }
    bestPathsCosts(path->currentPixel, pathNum) = PathCost{bestCost, bestDisp};
}

void SgmPathsManager::initBorderPixelGetters()
{
    borderPixelGetters[PathDirection::PosX] = &SgmPath_PosX::getBorderPixel;
    borderPixelGetters[PathDirection::NegX] = &SgmPath_NegX::getBorderPixel;
    borderPixelGetters[PathDirection::PosY] = &SgmPath_PosY::getBorderPixel;
    borderPixelGetters[PathDirection::NegY] = &SgmPath_NegY::getBorderPixel;
    borderPixelGetters[PathDirection::PosX_PosY] = &SgmPath_PosX_PosY::getBorderPixel;
    borderPixelGetters[PathDirection::NegX_PosY] = &SgmPath_NegX_PosY::getBorderPixel;
    borderPixelGetters[PathDirection::PosX_NegY] = &SgmPath_PosX_NegY::getBorderPixel;
    borderPixelGetters[PathDirection::NegX_NegY] = &SgmPath_NegX_NegY::getBorderPixel;
}

}
