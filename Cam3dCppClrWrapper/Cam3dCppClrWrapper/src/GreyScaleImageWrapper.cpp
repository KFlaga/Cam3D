#include "Stdafx.h"
#include "GreyScaleImageWrapper.h"

namespace Cam3dWrapper
{
	GreyScaleImageWrapper::GreyScaleImageWrapper(int rows_, int cols_) :
		rows(rows_),
		cols(cols_)
	{
		native = new cam3d::GreyScaleImage(rows, cols);
		matrix = gcnew cli::array<double, 2>(rows, cols);
	}

	GreyScaleImageWrapper::~GreyScaleImageWrapper()
	{
		if (native != nullptr) { delete native; }
	}

	void GreyScaleImageWrapper::Update()
	{
		for (int r = 0; r < rows; ++r)
		{
			for (int c = 0; c < cols; ++c)
			{
				(*native)(r, c) = matrix[r, c];
			}
		}
	}

	void GreyScaleImageWrapper::updateNative()
	{
		for (int r = 0; r < rows; ++r)
		{
			for (int c = 0; c < cols; ++c)
			{
				matrix[r, c] = (*native)(r, c);
			}
		}
	}
}