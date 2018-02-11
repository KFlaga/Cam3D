#pragma once

#include "GreyScaleImageWrapper.h"
#include "MaskedImageWrapper.h"
#include "DisparityMapWrapper.h"
#include <CamImageMatching\SgmCommon.hpp>
#include "Wrapper.h"

namespace Cam3dWrapper
{
	public enum class ImageType : int
	{
        Grey = (int)cam3d::ImageType::Grey,
		Color = (int)cam3d::ImageType::Color,
		MaskedGrey = (int)cam3d::ImageType::MaskedGrey,
		MaskedColor = (int)cam3d::ImageType::MaskedColor,
	};

	public enum class DisparityCostMethod : int
	{
		DistanceToMean = (int)cam3d::CostMethod::DistanceToMean,
		DistanceSquredToMean = (int)cam3d::CostMethod::DistanceSquredToMean,
	};

	public enum class DisparityMeanMethod : int
	{
		SimpleAverage = (int)cam3d::MeanMethod::SimpleAverage,
		WeightedAverageWithPathLength = (int)cam3d::MeanMethod::WeightedAverageWithPathLength,
	};

	public ref class SgmParameters
	{
	public:
		ImageType imageType;
		IWrapper^ leftImageWrapper;
		IWrapper^ rightImageWrapper;

		int rows;
		int cols;

		int maxParallelTasks;
		int maxDisparity;
		double lowPenaltyCoeff;
		double highPenaltyCoeff;
		double intensityThreshold;
		int censusMaskRadius;

		DisparityCostMethod disparityCostMethod;
		DisparityMeanMethod disparityMeanMethod;
		double diparityPathLengthThreshold;
		double costMethodPower;

	internal:
		cam3d::SgmParameters toNative();
	};

	public ref class SgmMatchingAlgorithm
	{
	public:
		SgmMatchingAlgorithm();
		~SgmMatchingAlgorithm();

		DisparityMapWrapper^ GetMapLeft() { return mapLeftToRight; }
		DisparityMapWrapper^ GetMapRight() { return mapRightToLeft; }

		void Process(SgmParameters^ parameters);
		void Terminate();
		System::String^ GetStatus();

	private:
		cam3d::ISgmCostAggregator* createSgm(SgmParameters^ parameters);

		DisparityMapWrapper^ mapLeftToRight;
		DisparityMapWrapper^ mapRightToLeft;
		SgmParameters^ parameters;
		cam3d::ISgmCostAggregator* sgm;
	};
}
