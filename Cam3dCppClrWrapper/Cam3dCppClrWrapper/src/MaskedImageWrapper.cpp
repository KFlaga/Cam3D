#include "Stdafx.h"
#include "MaskedImageWrapper.h"

namespace Cam3dWrapper
{
	GreyMaskedImageWrapper::GreyMaskedImageWrapper(int rows, int cols, Wrapper<cam3d::GreyScaleImage>^ image_) :
		MaskedImageWrapper<cam3d::GreyScaleImage>(rows, cols, image_)
	{ 
		
	}

	ColorMaskedImageWrapper::ColorMaskedImageWrapper(int rows, int cols, Wrapper<cam3d::ColorImage>^ image_) :
		MaskedImageWrapper(rows, cols, image_)
	{ 
	
	}

	// Hack needed for C# to see template code:
	void hack()
	{
		GreyMaskedImageWrapper gmiw{ 1, 1, nullptr };
		gmiw.GetImage();
		gmiw.GetMask();
		gmiw.SetMask(nullptr);
		gmiw.Update();
		gmiw.GetCols();
		gmiw.GetRows();

		ColorMaskedImageWrapper cmiw{ 1, 1, nullptr };
		cmiw.GetImage();
		cmiw.GetMask();
		cmiw.SetMask(nullptr);
		cmiw.Update();
		cmiw.GetCols();
		cmiw.GetRows();
	}
}