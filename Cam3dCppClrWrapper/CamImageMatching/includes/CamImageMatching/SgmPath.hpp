#pragma once

#include <CamCommon/Vector2.hpp>
#include <vector>

namespace cam3d
{
namespace PathDirections
{
enum PathDirection_ : int
{
    PosX, NegX, PosY, NegY,
    PosX_PosY, NegX_PosY, PosX_NegY, NegX_NegY,
    PosX2_PosY, NegX2_PosY, PosX2_NegY, NegX2_NegY,
    PosX_PosY2, NegX_PosY2, PosX_NegY2, NegX_NegY2,
};
}
using PathDirection = PathDirections::PathDirection_;

class SgmPath
{
public:
    int imageWidth;
    int imageHeight;
    int length;

    Point2 startPixel;
    Point2 currentPixel;
    Point2 previousPixel;
    int currentIndex;

    bool haveNextPixel()
    {
        return currentIndex < length - 1;
    }

    std::vector<double> lastStepCosts; // Needs to be allocated externally

    virtual void init() = 0;
    virtual void next() = 0;

    virtual ~SgmPath() { }
};

class SgmPath_PosX : public SgmPath
{
public:
	void init();
	void next();
	static Point2 getBorderPixel(Point2 pixel, int rows, int cols);
};

class SgmPath_PosY : public SgmPath
{
public:
	void init();
	void next();
	static Point2 getBorderPixel(Point2 pixel, int rows, int cols);
};

class SgmPath_NegX : public SgmPath
{
public:
	void init();
	void next();
	static Point2 getBorderPixel(Point2 pixel, int rows, int cols);
};

class SgmPath_NegY : public SgmPath
{
public:
	void init();
	void next();
	static Point2 getBorderPixel(Point2 pixel, int rows, int cols);
};

class SgmPath_PosX_PosY : public SgmPath
{
public:
	void init();
	void next();
	static Point2 getBorderPixel(Point2 pixel, int rows, int cols);
};

class SgmPath_PosX_NegY : public SgmPath
{
public:
	void init();
	void next();
	static Point2 getBorderPixel(Point2 pixel, int rows, int cols);
};

class SgmPath_NegX_PosY : public SgmPath
{
public:
	void init();
	void next();
	static Point2 getBorderPixel(Point2 pixel, int rows, int cols);
};

class SgmPath_NegX_NegY : public SgmPath
{
public:
	void init();
	void next();
	static Point2 getBorderPixel(Point2 pixel, int rows, int cols);
};
}
