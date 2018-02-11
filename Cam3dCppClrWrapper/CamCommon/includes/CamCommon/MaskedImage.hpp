#pragma once

#include <CamCommon/Array2d.hpp>
#include <type_traits>

namespace cam3d
{
template<typename Image>
class MaskedImage
{
public:
	using ImageType = Image;
	using Matrix = typename Image::Matrix;

private:
	Image& image;
    Array2d<char> mask;

public:
    MaskedImage(Image& image_) :
        image(image_),
        mask{ image_.getRowCount(), image_.getColumnCount() }
    {
        mask.fill(false);
    }

    double operator()(int y, int x) const { return image(y, x); }
    double& operator()(int y, int x) { return image(y, x); }
    double operator()(int y, int x, int channel) const { return image(y, x, channel); }
    double& operator()(int y, int x, int channel) { return image(y, x, channel); }

    int getColumnCount() const { return image.getColumnCount(); }
    int getRowCount() const { return image.getRowCount(); }
    int getChannelsCount() const { return image.getChannelsCount(); }

    bool haveValueAt(int y, int x) { return static_cast<bool>(mask(y, x)); }
    void setMaskAt(int y, int x, bool value) { mask(y, x) = static_cast<char>(value); }

    Matrix& getMatrix() { return image.getMatrix(); }
    const Matrix& getMatrix() const { return image.getMatrix(); }
};
}
