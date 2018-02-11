#include "SgmCommon.hpp"
#include "ParallelSgmAlgorithm.hpp"
#include "CensusCostComputer.hpp"
#include <CamCommon\GreyScaleImage.hpp>
#include <CamCommon\MaskedImage.hpp>
#include <CamCommon\ColorImage.hpp>

namespace cam3d
{
	template<int maskRadius, int maxRadiusPlusOne, typename ImageT>
	struct SgmCreator
	{
		static cam3d::ISgmCostAggregator* create(SgmParameters& parameters, DisparityMap& mapLeft, DisparityMap& mapRight, ImageT& imageLeft, ImageT& imageRight)
		{
			if (maskRadius == parameters.censusMaskRadius)
			{
				return new ParallelSgmAlgorithm<cam3d::SgmCostAggregator<ImageT, cam3d::CensusCostComputer32<ImageT, maskRadius>>>{
					parameters, mapLeft, mapRight, imageLeft, imageRight
				};
			}
			else
			{
				return SgmCreator<maskRadius + 1, maxRadiusPlusOne, ImageT>::create(parameters, mapLeft, mapRight, imageLeft, imageRight);
			}
		}
	};

	template<int rmax, typename ImageT>
	struct SgmCreator<rmax, rmax, ImageT>
	{
		static cam3d::ISgmCostAggregator* create(SgmParameters& parameters, DisparityMap& mapLeft, DisparityMap& mapRight, ImageT& imageLeft, ImageT& imageRight)
		{
			throw std::invalid_argument(std::string("Census mask radius must be in range [1, ") + std::to_string(rmax - 1) + "].");
		}
	};

	ISgmCostAggregator* createSgm(SgmParameters& parameters, DisparityMap& mapLeft, DisparityMap& mapRight, void* imageLeft, void* imageRight)
	{
		parameters.censusMaskRadius = parameters.censusMaskRadius > 7 ? 7 : parameters.censusMaskRadius;
		cam3d::ISgmCostAggregator* sgm = nullptr;
		if (parameters.imageType == ImageType::Grey)
		{
			sgm = SgmCreator<1, 8, cam3d::GreyScaleImage>::create(parameters, mapLeft, mapRight, *reinterpret_cast<GreyScaleImage*>(imageLeft), *reinterpret_cast<GreyScaleImage*>(imageRight));
		}
		else if (parameters.imageType == ImageType::MaskedGrey)
		{
			using MaskedImage = cam3d::MaskedImage<cam3d::GreyScaleImage>;
			sgm = SgmCreator<1, 8, MaskedImage>::create(parameters, mapLeft, mapRight, *reinterpret_cast<MaskedImage*>(imageLeft), *reinterpret_cast<MaskedImage*>(imageRight));
		}
		else
		{
			throw std::invalid_argument("Only GrayScaleImage or masked GreyScaleImage supported");
		}
		return sgm;
	}
}