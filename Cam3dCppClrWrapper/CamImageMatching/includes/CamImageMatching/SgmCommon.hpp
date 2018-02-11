#pragma once

#include <CamCommon/Vector2.hpp>
#include <CamCommon/Array2d.hpp>
#include <CamCommon/Array3d.hpp>
#include <CamCommon/DisparityMap.hpp>
#include <array>
#include <vector>
#include <functional>
#include <algorithm>

namespace cam3d
{
	enum class MeanMethod
	{
        SimpleAverage,
		WeightedAverageWithPathLength,
	};

	enum class CostMethod
	{
		DistanceToMean,
        DistanceSquredToMean,
	};

	enum class ImageType
	{
		Grey,
		Color,
		MaskedGrey,
		MaskedColor
	};

    struct PathCost
	{
		double cost;
		int disparity;
        int pathLength;
	};

	enum class RunDirection
	{
		TopDown,
		BottomUp
	};

	constexpr int pathsCount = 8;

	struct SgmParameters
	{
		int rows;
		int cols;
		ImageType imageType;
		bool isLeftImageBase;
		int maxParallelTasks;

        int maxDisparity;
		double lowPenaltyCoeff;
		double highPenaltyCoeff;
        double intensityThreshold;
		int censusMaskRadius;
		MeanMethod disparityMeanMethod;
		CostMethod disparityCostMethod;
		double diparityPathLengthThreshold;
        double costMethodPower;
	};

	class ISgmCostAggregator
	{
	public:
		virtual void computeMatchingCosts() = 0;
		virtual void terminate() = 0;
		virtual std::string getState() = 0;
	};

	ISgmCostAggregator* createSgm(SgmParameters& parameters, DisparityMap& mapLeft, DisparityMap& mapRight, void* imageLeft, void* imageRight);
}
