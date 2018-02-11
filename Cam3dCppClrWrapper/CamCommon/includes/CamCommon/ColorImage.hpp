#pragma once

#include <CamCommon/Array3d.hpp>

namespace cam3d
{
    class ColorImage
    {
	public:
		using Matrix = Array3d<double>;

    private:
		Matrix imageMatrix;

    public:
		ColorImage(int rows, int cols) :
			imageMatrix{ rows, cols, 3 }
		{ }

        double operator()(int y, int x) const { return imageMatrix(y, x, 0); }
        double& operator()(int y, int x) { return imageMatrix(y, x, 0); }
        double operator()(int y, int x, int channel) const { return imageMatrix(y, x, channel); }
        double& operator()(int y, int x, int channel) { return imageMatrix(y, x, channel); }

        int getColumnCount() const { return imageMatrix.getColumnCount(); }
        int getRowCount() const { return imageMatrix.getRowCount(); }
		int getChannelsCount() const { return 3; }

        bool haveValueAt(int y, int x) { return true; }

		Matrix& getMatrix() { return imageMatrix; }
        const Matrix& getMatrix() const { return imageMatrix; }
    };
}