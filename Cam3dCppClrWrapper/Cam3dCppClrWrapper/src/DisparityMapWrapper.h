#pragma once

#include <CamCommon\DisparityMap.hpp>
#include "DisparityWrapper.h"
#include "Wrapper.h"

namespace Cam3dWrapper
{
	public ref class DisparityMapWrapper : public Wrapper<cam3d::DisparityMap>
	{
	public:
		DisparityMapWrapper(int rows, int cols);
		~DisparityMapWrapper();

		cli::array<DisparityWrapper^, 2>^ GetDisparities()
		{
			return map;
		}

		void SetDisparities(cli::array<DisparityWrapper^, 2>^ map_)
		{
			map = map_;
			Update();
		}

		virtual void Update() override;

		int GetRows() { return rows; }
		int GetCols() { return cols; }

	internal:
		virtual void updateNative() override;

	private:
		cli::array<DisparityWrapper^, 2>^ map;
		int rows;
		int cols;
	};
}