#include "Stdafx.h"
#include "SgmMatchingAlgorithm.h"

namespace Cam3dWrapper
{
	SgmMatchingAlgorithm::SgmMatchingAlgorithm()
	{
		sgm = nullptr;
	}

	SgmMatchingAlgorithm::~SgmMatchingAlgorithm()
	{
		if (sgm != nullptr)
		{
			delete sgm;
			sgm = nullptr;
		}
	}

	void SgmMatchingAlgorithm::Process(SgmParameters^ parameters_)
	{
		parameters = parameters_;

		if (sgm != nullptr)
		{
			delete sgm;
		}

		mapLeftToRight = gcnew DisparityMapWrapper{ parameters->rows, parameters->cols };
		mapRightToLeft = gcnew DisparityMapWrapper{ parameters->rows, parameters->cols };

		sgm = createSgm(parameters);
		sgm->computeMatchingCosts();
		mapLeftToRight->updateNative();
		mapRightToLeft->updateNative();
		auto* sgmLeftOver = sgm;
		sgm = nullptr;
		delete sgmLeftOver;
	}

	void SgmMatchingAlgorithm::Terminate()
	{
		if (sgm != nullptr)
		{
			sgm->terminate();
		}
	}

	System::String^ SgmMatchingAlgorithm::GetStatus()
	{
		if (sgm != nullptr)
		{
			std::string status = sgm->getState();
			System::String^ res = gcnew System::String(status.c_str());

			return res;
		}
		return gcnew System::String("");
	}

	cam3d::ISgmCostAggregator* SgmMatchingAlgorithm::createSgm(SgmParameters^ parameters)
	{
		cam3d::SgmParameters nativeParameters = parameters->toNative();

		return cam3d::createSgm(nativeParameters, 
			*mapLeftToRight->getNativeAs<cam3d::DisparityMap>(),
			*mapRightToLeft->getNativeAs<cam3d::DisparityMap>(),
			parameters->leftImageWrapper->getNative(), 
			parameters->rightImageWrapper->getNative());
	}

	cam3d::SgmParameters SgmParameters::toNative()
	{
        cam3d::SgmParameters p;
		p.maxParallelTasks = this->maxParallelTasks;
		p.maxDisparity = this->maxDisparity;
		p.censusMaskRadius = this->censusMaskRadius;
		p.lowPenaltyCoeff = this->lowPenaltyCoeff;
		p.highPenaltyCoeff = this->highPenaltyCoeff;
		p.intensityThreshold = this->intensityThreshold;
        p.disparityCostMethod = (cam3d::CostMethod)this->disparityCostMethod;
        p.disparityMeanMethod = (cam3d::MeanMethod)this->disparityMeanMethod;
		p.diparityPathLengthThreshold = this->diparityPathLengthThreshold;
		p.costMethodPower = this->costMethodPower;
		p.imageType = (cam3d::ImageType)this->imageType;
		p.rows = this->rows;
		p.cols = this->cols;
		return p;
	}
}
