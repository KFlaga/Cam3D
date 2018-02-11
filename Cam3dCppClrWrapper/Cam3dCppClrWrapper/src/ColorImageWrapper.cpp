#include "Stdafx.h"
#include "ColorImageWrapper.h"

namespace Cam3dWrapper
{
	ColorImageWrapper::ColorImageWrapper(int rows_, int cols_) :
		rows(rows_),
		cols(cols_)
	{
		native = new cam3d::ColorImage(rows, cols);
		matrix = gcnew cli::array<double, 3>(rows, cols, 3);
	}

	ColorImageWrapper::~ColorImageWrapper()
	{
		if (native != nullptr) { delete native; }
	}

	void ColorImageWrapper::Update()
	{
		for (int r = 0; r < rows; ++r)
		{
			for (int c = 0; c < cols; ++c)
			{
				for (int d = 0; d < 3; ++d)
				{
					(*native)(r, c, d) = matrix[r, c, d];
				}
			}
		}
	}

	void ColorImageWrapper::updateNative()
	{
		for (int r = 0; r < rows; ++r)
		{
			for (int c = 0; c < cols; ++c)
			{
				for (int d = 0; d < 3; ++d)
				{
					matrix[r, c, d] = (*native)(r, c, d);
				}
			}
		}
	}
}