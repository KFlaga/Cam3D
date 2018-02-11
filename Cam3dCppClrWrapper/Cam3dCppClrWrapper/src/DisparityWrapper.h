#pragma once

#include <CamCommon\Disparity.hpp>

namespace Cam3dWrapper
{
	public enum class DisparityFlagsWrapper
	{
		Valid = cam3d::Disparity::Valid,
		Invalid = cam3d::Disparity::Invalid,
		Occluded = cam3d::Disparity::Occluded
	};

	public value struct DisparityWrapper
	{
	public:
		int dx;
		int flags;
		double subDx;
		double cost;
		double confidence;

	internal:
		static DisparityWrapper^ _fromNative(cam3d::Disparity d)
		{
			DisparityWrapper^ dw = gcnew DisparityWrapper();
			{
				dw->dx = d.dx;
				dw->flags = d.flags;
				dw->subDx = d.subDx;
				dw->cost = d.cost;
				dw->confidence = d.confidence;
			}
			return dw;
		}

		static cam3d::Disparity _toNative(DisparityWrapper^ d)
		{
			return cam3d::Disparity{
				d->dx,
				(cam3d::Disparity::DisparityFlags)d->flags,
				d->subDx,
				d->cost,
				d->confidence
			};
		}
	};
}