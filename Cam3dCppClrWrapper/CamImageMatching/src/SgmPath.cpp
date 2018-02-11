#include "SgmPath.hpp"
#include <algorithm>

namespace cam3d
{
	namespace
	{
		template<typename T>
		static constexpr T getAbs(const T i)
		{
			return i < 0 ? -i : i;
		}

		template<int moveY, int moveX>
		struct length_helper
		{
		public:
			static int getLength(int length, int rows, int cols, Point2 startPixel)
			{
				return std::min(
					getLength2<0, moveX>(length, rows, cols, startPixel),
					getLength2<moveY, 0>(length, rows, cols, startPixel)
				);
			}

			template<int y, int x>
			static typename std::enable_if<(x == 0) && (y == 0), int>::type getLength2(
				int length, int rows, int cols, Point2 startPixel)
			{
				return length;
			}

			template<int y, int x>
			static typename std::enable_if<(x > 0) && (y == 0), int>::type getLength2(
				int length, int rows, int cols, Point2 startPixel)
			{
				return (cols - startPixel.x) * x; // on 2 -> (c - x) * 2
			}

			template<int y, int x>
			static typename std::enable_if<(x < 0) && (y == 0), int>::type getLength2(
				int length, int rows, int cols, Point2 startPixel)
			{
				return (startPixel.x + 1) * (-x); // on 2 -> x * 2 + 1
			}

			template<int y, int x>
			static typename std::enable_if<(x == 0) && (y > 0), int>::type getLength2(
				int length, int rows, int cols, Point2 startPixel)
			{
				return (rows - startPixel.y) * y;
			}

			template<int y, int x>
			static typename std::enable_if<(x == 0) && (y < 0), int>::type getLength2(
				int length, int rows, int cols, Point2 startPixel)
			{
				return (startPixel.y + 1) * (-y);
			}
		};

		template<int moveY, int moveX>
		struct move_helper
		{
			static Point2 getMove(int currentIndex)
			{
				return Point2{
					std::conditional<getAbs(moveY) == 2, Move2<moveY>, Move01<moveY>>::type::getMove(currentIndex),
					std::conditional<getAbs(moveX) == 2, Move2<moveX>, Move01<moveX>>::type::getMove(currentIndex)
				};
			}

			template<int m>
			struct Move2
			{
				static int getMove(int currentIndex)
				{
					static const int m2 = m / 2;
					return (currentIndex & 1) == 0 ? 0 : m2;
				}
			};

			template<int m>
			struct Move01
			{
				static int getMove(int currentIndex)
				{
					return m;
				}
			};
		};

		template<int moveY, int moveX>
		struct border_helper
		{
			static Point2 getPixel(Point2 pixel, int rows, int cols)
			{
				return std::conditional<(getAbs(moveX) < 2) && (getAbs(moveY) < 2), Pixel01, Pixel2
				>::type::getPixel(pixel, rows, cols);
			}

			struct Pixel01
			{
				static Point2 getPixel(Point2 pixel, int rows, int cols)
				{
					int d = Pixel01::getMoveLimit(pixel, rows, cols);
					return Point2{ pixel.y + d * (-moveY), pixel.x + d * (-moveX) };
				}

				static int getMoveLimit(Point2 pixel, int rows, int cols)
				{
					return std::min(
						getMoveLimit1<moveY, 0>(pixel, rows, cols),
						getMoveLimit1<0, moveX>(pixel, rows, cols)
					);
				}
			};

			struct Pixel2
			{
				static Point2 getPixel(Point2 pixel, int rows, int cols)
				{
					return std::conditional<getAbs(moveX) == 2, X2, Y2>::type::getPixel(pixel, rows, cols);
				}

				struct X2
				{
					static Point2 getPixel(Point2 pixel, int rows, int cols)
					{
						return{ 0, 0 };
					}
				};

				struct Y2
				{
					static Point2 getPixel(Point2 pixel, int rows, int cols)
					{
						// Move xy,x,xy,x
						// check if is bounded by x (compare lengths of opposite move)
						if (length_helper::getLength2(10000, rows, cols, pixel) >=
							length_helper::getLength2(10000, rows, cols, pixel))
						{
							int d = getMoveLimit1<moveY / 2, moveX / 2>(pixel, rows, cols);
							return Point2{ pixel.y + d * (-moveY) / 4, pixel.x + d * (-moveX) };
						}
					}
				};
			};

			template<int y, int x>
			static typename std::enable_if<(x == 0) && (y == 0), int>::type getMoveLimit1(Point2 pixel, int rows, int cols)
			{
				return 10000;
			}

			template<int y, int x>
			static typename std::enable_if<(x == 1) && (y == 0), int>::type getMoveLimit1(Point2 pixel, int rows, int cols)
			{
				return pixel.x;
			}

			template<int y, int x>
			static typename std::enable_if<(x == -1) && (y == 0), int>::type getMoveLimit1(Point2 pixel, int rows, int cols)
			{
				return cols - pixel.x - 1;
			}

			template<int y, int x>
			static typename std::enable_if<(x == 0) && (y == 1), int>::type getMoveLimit1(Point2 pixel, int rows, int cols)
			{
				return pixel.y;
			}

			template<int y, int x>
			static typename std::enable_if<(x == 0) && (y == -1), int>::type getMoveLimit1(Point2 pixel, int rows, int cols)
			{
				return rows - pixel.y - 1;
			}
		};
	}

	template<int moveY, int moveX>
	struct SgmPathFuncs
	{
		template<typename SgmPathT>
		inline static void init(SgmPathT& path)
		{
			path.currentIndex = 0;
			path.currentPixel = path.startPixel;
			path.previousPixel = path.currentPixel;
			path.length = length_helper<moveY, moveX>::getLength(path.length, path.imageHeight, path.imageWidth, path.startPixel);
		}

		template<typename SgmPathT>
		inline static void next(SgmPathT& path)
		{
			path.previousPixel = path.currentPixel;
			path.currentPixel = path.currentPixel + move_helper<moveY, moveX>::getMove(path.currentIndex);
			++path.currentIndex;
		}
	};

	void SgmPath_PosX::init()
	{
		SgmPathFuncs<0, 1>::init(*this);
	}

	void SgmPath_PosX::next()
	{
		SgmPathFuncs<0, 1>::next(*this);
	}

	Point2 SgmPath_PosX::getBorderPixel(Point2 pixel, int rows, int cols)
	{
		return border_helper<0, 1>::getPixel(pixel, rows, cols);
	}

	void SgmPath_PosY::init()
	{
		SgmPathFuncs<1, 0>::init(*this);
	}

	void SgmPath_PosY::next()
	{
		SgmPathFuncs<1, 0>::next(*this);
	}

	Point2 SgmPath_PosY::getBorderPixel(Point2 pixel, int rows, int cols)
	{
		return border_helper<1, 0>::getPixel(pixel, rows, cols);
	}

	void SgmPath_NegX::init()
	{
		SgmPathFuncs<0, -1>::init(*this);
	}

	void SgmPath_NegX::next()
	{
		SgmPathFuncs<0, -1>::next(*this);
	}

	Point2 SgmPath_NegX::getBorderPixel(Point2 pixel, int rows, int cols)
	{
		return border_helper<0, -1>::getPixel(pixel, rows, cols);
	}

	void SgmPath_NegY::init()
	{
		SgmPathFuncs<-1, 0>::init(*this);
	}

	void SgmPath_NegY::next()
	{
		SgmPathFuncs<-1, 0>::next(*this);
	}

	Point2 SgmPath_NegY::getBorderPixel(Point2 pixel, int rows, int cols)
	{
		return border_helper<-1, 0>::getPixel(pixel, rows, cols);
	}

	void SgmPath_PosX_PosY::init()
	{
		SgmPathFuncs<1, 1>::init(*this);
	}

	void SgmPath_PosX_PosY::next()
	{
		SgmPathFuncs<1, 1>::next(*this);
	}

	Point2 SgmPath_PosX_PosY::getBorderPixel(Point2 pixel, int rows, int cols)
	{
		return border_helper<1, 1>::getPixel(pixel, rows, cols);
	}

	void SgmPath_PosX_NegY::init()
	{
		SgmPathFuncs<-1, 1>::init(*this);
	}

	void SgmPath_PosX_NegY::next()
	{
		SgmPathFuncs<-1, 1>::next(*this);
	}

	Point2 SgmPath_PosX_NegY::getBorderPixel(Point2 pixel, int rows, int cols)
	{
		return border_helper<-1, 1>::getPixel(pixel, rows, cols);
	}

	void SgmPath_NegX_PosY::init()
	{
		SgmPathFuncs<1, -1>::init(*this);
	}

	void SgmPath_NegX_PosY::next()
	{
		SgmPathFuncs<1, -1>::next(*this);
	}

	Point2 SgmPath_NegX_PosY::getBorderPixel(Point2 pixel, int rows, int cols)
	{
		return border_helper<1, -1>::getPixel(pixel, rows, cols);
	}

	void SgmPath_NegX_NegY::init()
	{
		SgmPathFuncs<-1, -1>::init(*this);
	}

	void SgmPath_NegX_NegY::next()
	{
		SgmPathFuncs<-1, -1>::next(*this);
	}

	Point2 SgmPath_NegX_NegY::getBorderPixel(Point2 pixel, int rows, int cols)
	{
		return border_helper<-1, -1>::getPixel(pixel, rows, cols);
	}
}
