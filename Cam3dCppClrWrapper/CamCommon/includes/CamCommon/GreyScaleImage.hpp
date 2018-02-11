#pragma once

#include <CamCommon/Array2d.hpp>

namespace cam3d
{
class GreyScaleImage
{
public:
	using Matrix = Array2d<double>;

private:
	Matrix imageMatrix;

public:
    GreyScaleImage(int rows, int cols) :
		imageMatrix{rows, cols}
	{ }

    double operator()(int y, int x) const { return imageMatrix(y,x); }
    double& operator()(int y, int x) { return imageMatrix(y,x); }
    double operator()(int y, int x, int channel) const { return imageMatrix(y,x); }
    double& operator()(int y, int x, int channel) { return imageMatrix(y,x); }

    int getColumnCount() const { return imageMatrix.getColumnCount(); }
    int getRowCount() const { return imageMatrix.getRowCount(); }
    int getChannelsCount() const { return 1; }

    bool haveValueAt(int y, int x) { return true; }

	Matrix& getMatrix() { return imageMatrix; }
    const Matrix& getMatrix() const { return imageMatrix; }
};
}
