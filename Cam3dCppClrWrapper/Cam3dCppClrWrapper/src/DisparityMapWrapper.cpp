#include "Stdafx.h"
#include "DisparityMapWrapper.h"

namespace Cam3dWrapper
{
	DisparityMapWrapper::DisparityMapWrapper(int rows_, int cols_) :
		rows(rows_),
		cols(cols_)
	{
		native = new cam3d::DisparityMap(rows, cols);
		map = gcnew cli::array<DisparityWrapper^, 2>(rows, cols);
	}

	DisparityMapWrapper::~DisparityMapWrapper()
	{
		delete native;
	}

	void DisparityMapWrapper::Update()
	{
		for (int r = 0; r < rows; ++r)
		{
			for (int c = 0; c < cols; ++c)
			{
				(*native)(r, c) = DisparityWrapper::_toNative(map[r, c]);
			}
		}
	}

	void DisparityMapWrapper::updateNative()
	{
		for (int r = 0; r < rows; ++r)
		{
			for (int c = 0; c < cols; ++c)
			{
				map[r, c] = DisparityWrapper::_fromNative((*native)(r, c));
			}
		}
	}
}