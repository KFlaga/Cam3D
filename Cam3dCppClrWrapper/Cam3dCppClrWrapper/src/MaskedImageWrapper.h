#pragma once

#include <CamCommon\MaskedImage.hpp>
#include "GreyScaleImageWrapper.h"
#include "ColorImageWrapper.h"
#include "Wrapper.h"

namespace Cam3dWrapper
{
	template<typename ImageT>
	public ref class MaskedImageWrapper : public Wrapper<cam3d::MaskedImage<ImageT>>
	{
	public:
		MaskedImageWrapper(int rows_, int cols_, Wrapper<ImageT>^ image_) :
			rows{ rows_ },
			cols{ cols_ },
			image{ image_ }
		{
			mask = gcnew cli::array<bool, 2>(rows, cols);
			native = new cam3d::MaskedImage<ImageT>(*image->getNativeAs<ImageT>());
			updateNative();
		}

		~MaskedImageWrapper()
		{
			delete native;
		}

		Wrapper<ImageT>^ GetImage()
		{
			return image;
		}

		cli::array<bool, 2>^ GetMask()
		{
			return mask;
		}

		void SetMask(cli::array<bool, 2>^ mask_)
		{
			mask = mask_;
			Update();
		}

		virtual void Update() override;

		int GetRows() { return rows; }
		int GetCols() { return cols; }

	internal:
		virtual void updateNative() override;

	private:
		cli::array<bool, 2>^ mask;
		Wrapper<ImageT>^ image;
		int rows;
		int cols;
	};

	template<typename ImageT>
	void MaskedImageWrapper<ImageT>::Update()
	{
		image->Update();
		for (int r = 0; r < rows; ++r)
		{
			for (int c = 0; c < cols; ++c)
			{
				native->setMaskAt(r, c , mask[r, c]);
			}
		}
	}

	template<typename ImageT>
	void MaskedImageWrapper<ImageT>::updateNative()
	{
		image->updateNative();
		for (int r = 0; r < rows; ++r)
		{
			for (int c = 0; c < cols; ++c)
			{
				mask[r, c] = native->haveValueAt(r, c);
			}
		}
	}
	
	public ref class GreyMaskedImageWrapper : public MaskedImageWrapper<cam3d::GreyScaleImage>
	{
	public:
		GreyMaskedImageWrapper(int rows, int cols, Wrapper<cam3d::GreyScaleImage>^ image_);
	};

	public ref class ColorMaskedImageWrapper : public MaskedImageWrapper<cam3d::ColorImage>
	{
	public:
		ColorMaskedImageWrapper(int rows, int cols, Wrapper<cam3d::ColorImage>^ image_);
	};
}