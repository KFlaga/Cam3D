#pragma once

#include <CamCommon\GreyScaleImage.hpp>
#include "Wrapper.h"

namespace Cam3dWrapper
{
	public ref class GreyScaleImageWrapper : public Wrapper<cam3d::GreyScaleImage>
	{
	public:
		GreyScaleImageWrapper(int rows, int cols);
		~GreyScaleImageWrapper();

		void SetMatrix(cli::array<double, 2>^ matrix_)
		{
			matrix = matrix_;
			Update();
		}

		cli::array<double, 2>^ GetMatrix()
		{
			return matrix;
		}

		virtual void Update() override;

		int GetRows() { return rows; }
		int GetCols() { return cols; }

	internal:
		virtual void updateNative() override;
		
	private:
		cli::array<double, 2>^ matrix;
		int rows;
		int cols;
	};
}