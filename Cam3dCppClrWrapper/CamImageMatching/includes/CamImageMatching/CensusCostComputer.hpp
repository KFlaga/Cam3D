#pragma once

#include <CamCommon/Array2d.hpp>
#include <CamCommon/PerPixelFunction.hpp>
#include <CamCommon/BitWord.hpp>
#include <cstring>

namespace cam3d
{
// TODO:: add support for masked image
template<typename Image, typename BitWord>
class CensusCostComputer
{
public:
    using uint_t = typename BitWord::uint_t;
    using BitWordMatrix = Array2d<BitWord>;

private:
    int rows;
    int cols;
    int maskLength;
    double maxCost;

    BitWordMatrix censusBase;
    BitWordMatrix censusMatched;

    int maskWidth; // Actual width is equal to maskWidth*2 + 1
    int maskHeight; // Actual height is equal to maskHeight*2 + 1

	Point2 currentPixel;

public:
    CensusCostComputer(int rows_, int cols_) :
        rows{rows_},
        cols{cols_},
        censusBase{rows_, cols_},
        censusMatched{rows_, cols_},
		currentPixel{}
    {

    }

	Point2 getCurrentPixel() const
	{
		return currentPixel;
	}

    double getCost(Point2 pixelBase, Point2 pixelMatched)
    {
        return censusBase(pixelBase.y, pixelBase.x).getHammingDistance(
                    censusMatched(pixelMatched.y, pixelMatched.x));
    }

    double getCostOnBorder(Point2 pixelBase, Point2 pixelMatched)
    {
        return getCost(pixelBase, pixelMatched);
    }

    void init(Image& imageBase, Image& imageMatched)
    {
        // Transform images using census transform
        censusBase.clear();
        censusMatched.clear();

        maskLength = (2 * maskHeight + 1) * (2 * maskWidth + 1);
        maxCost = maskLength - 1;

		PerPixelFunction::runWithBorder
        (
            [this, &imageBase, &imageMatched](int y, int x) { censusTransform(y, x, imageBase, imageMatched); },
            [this, &imageBase, &imageMatched](int y, int x) { censusTransform_Border(y, x, imageBase, imageMatched); },
            maskWidth, maskHeight,
            imageBase.getRowCount(), imageBase.getColumnCount()
        );
    }

    int getMaskLength() const { return maskLength; }
    double getMaxCost() const { return maxCost; }

    const BitWordMatrix& getCensusBase() const { return censusBase; }
    const BitWordMatrix& getCensusMatched() const { return censusMatched; }

    int getMaskWidth() const { return maskWidth; }
    void setMaskWidth(int value) { maskWidth = value; }
    int getMaskHeight() const { return maskHeight; }
    void setMaskHeight(int value) { maskHeight = value; }

private:
    void censusTransform(int y, int x, Image& imageBase, Image& imageMatched)
    {
        uint_t maskBase[BitWord::lengthInWords];
        uint_t maskMatch[BitWord::lengthInWords];
        std::memset(maskBase, 0, sizeof(maskBase));
        std::memset(maskMatch, 0, sizeof(maskMatch));
		currentPixel.x = x;
		currentPixel.y = y;

        int dx, dy, maskPos = 0;
        for(dy = -maskHeight; dy <= maskHeight; ++dy)
        {
            for(dx = -maskWidth; dx <= maskWidth; ++dx)
            {
                if(imageBase(y + dy, x + dx) < imageBase(y, x))
                {
                    maskBase[maskPos / BitWord::bitSizeOfWord] |= (1u << (maskPos % BitWord::bitSizeOfWord));
                }
                if(imageMatched(y + dy, x + dx) < imageMatched(y, x))
                {
                    maskMatch[maskPos / BitWord::bitSizeOfWord] |= (1u << (maskPos % BitWord::bitSizeOfWord));
                }
                ++maskPos;
            }
        }

        censusBase(y, x) = BitWord{&maskBase[0]};
        censusMatched(y, x) = BitWord{&maskMatch[0]};
    }

    void censusTransform_Border(int y, int x, Image& imageBase, Image& imageMatched)
    {
        uint_t maskBase[BitWord::lengthInWords];
        uint_t maskMatch[BitWord::lengthInWords];
        std::memset(maskBase, 0, sizeof(maskBase));
        std::memset(maskMatch, 0, sizeof(maskMatch));
		currentPixel.x = x;
		currentPixel.y = y;

        int dx, dy, px, py, maskPos = 0;
        for(dy = -maskHeight; dy <= maskHeight; ++dy)
        {
            for(dx = -maskWidth; dx <= maskWidth; ++dx)
            {
                px = x + dx;
                px = px > imageBase.getColumnCount() - 1 ? 2 * imageBase.getColumnCount() - px - 2 : px;
                px = px < 0 ? -px : px;

                py = y + dy;
                py = py > imageMatched.getRowCount() - 1 ? 2 * imageMatched.getRowCount() - py - 2 : py;
                py = py < 0 ? -py : py;

                if(imageBase(py, px) < imageBase(y, x))
                {
                    maskBase[maskPos / BitWord::bitSizeOfWord] |= (1u << (maskPos % BitWord::bitSizeOfWord));
                }
                if(imageMatched(py, px) < imageMatched(y, x))
                {
                    maskMatch[maskPos / BitWord::bitSizeOfWord] |= (1u << (maskPos % BitWord::bitSizeOfWord));
                }
                ++maskPos;
            }
        }

        censusBase(y, x) = BitWord{&maskBase[0]};
        censusMatched(y, x) = BitWord{&maskMatch[0]};
    }
};

template<typename Image, int maskRadius>
using CensusCostComputer32 = CensusCostComputer<Image, BitWord32<((2 * maskRadius + 1) * (2 * maskRadius + 1) / 32) + 1>>;
}
