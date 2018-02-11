#pragma once

#include <CamCommon\ColorImage.hpp>
#include "Wrapper.h"

namespace Cam3dWrapper
{
	public ref class ColorImageWrapper : public Wrapper<cam3d::ColorImage>
	{
	public:
		ColorImageWrapper(int rows, int cols);
		~ColorImageWrapper();

		void SetMatrix(cli::array<double, 3>^ matrix_)
		{
			matrix = matrix_;
			Update();
		}

		cli::array<double, 3>^ GetMatrix()
		{
			return matrix;
		}

		virtual void Update() override;

		int GetRows() { return rows; }
		int GetCols() { return cols; }

	internal:
		virtual void updateNative() override;

	private:
		cli::array<double, 3>^ matrix;
		int rows;
		int cols;
	};
}