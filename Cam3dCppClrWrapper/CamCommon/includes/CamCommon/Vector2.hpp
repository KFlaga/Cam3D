#pragma once

#include "PreReqs.hpp"

namespace cam3d
{
	template<typename T>
	struct Vector2
	{
	public:
		T x;
		T y;

		constexpr Vector2() : x{ 0 }, y{ 0 } { }
		constexpr Vector2(T x_, T y_) : x{ x_ }, y{ y_ } { }

		Vector2& operator+=(Vector2 a)
		{
			x += a.x;
			y += a.y;
		}

		Vector2& operator-=(Vector2 a)
		{
			x += a.x;
			y += a.y;
		}

		Vector2& operator*=(T s)
		{
			x *= s;
			y *= s;
		}

		Vector2& operator/=(T s)
		{
			x /= s;
			y /= s;
		}

        constexpr double dot(const Vector2 v) const
		{
			return x * v.x + y * v.y;
		}

        constexpr double cross(const Vector2 v) const
		{
			return x * v.y - y * v.x;
		}

        constexpr double lengthSquared() const
		{
			return x * x + y * y;
		}

		double length() const
		{
			return std::sqrt(x * x + y * y);
		}

		void normalise()
		{
			double len = length();
			if (len > 0)
			{
				x /= len;
				y /= len;
			}
		}

        constexpr double distanceToSquared(const Vector2 v) const
		{
			return (x - v.x) * (x - v.x) + (y - v.y) * (y - v.y);
		}

        double distanceTo(const Vector2 v) const
		{
			return std::sqrt(distanceToSquared(v));
		}

		// Returns value in radians
        double angleTo(const Vector2 v) const
		{
			return std::asin(sinusTo(v));
		}

		// Returns value in radians. Assumes both vector are normalized
        double angleToNormalized(const Vector2 v) const
		{
			return std::asin(cross(v));
		}

		// Returns sinus of angle to v, value in radians
        double sinusTo(const Vector2 v) const
		{
			return cross(v) / std::sqrt(lengthSquared() * v.lengthSquared());
		}

		// Returns sinus of angle to v, value in radians. Assumes both vector are normalized
        double sinusToNormalized(const Vector2 v) const
		{
			return cross(v);
		}
		// Returns cosinus of angle to v, value in radians
        double cosinusTo(const Vector2 v) const
		{
			return dot(v) / std::sqrt(lengthSquared() * v.lengthSquared());
		}

		// Returns cosinus of angle to v, value in radians. Assumes both vector are normalized
        double cosinusToNormalized(const Vector2 v) const
		{
			return dot(v);
		}

		template<typename T2>
        constexpr operator Vector2<T2>() const
		{
			return Vector2<T2>{ static_cast<T2>(x), static_cast<T2>(y) };
		}

		template<typename T2>
		constexpr Vector2(const Vector2<T2>& v) : x{static_cast<T>(v.x)}, y{static_cast<T>(v.y)} { }
	};

	template<typename T>
	constexpr Vector2<T> operator+(Vector2<T> a, Vector2<T> b)
	{
		return Vector2<T>{ a.x + b.x, a.y + b.y };
	}

	template<typename T>
	constexpr Vector2<T> operator-(Vector2<T> a, Vector2<T> b)
	{
		return Vector2<T>{ a.x - b.x, a.y - b.y };
	}

	template<typename T>
	constexpr Vector2<T> operator*(Vector2<T> a, Vector2<T> b)
	{
		return Vector2<T>{ a.x * b.x, a.y * b.y };
	}

	template<typename T>
	constexpr Vector2<T> operator/(Vector2<T> a, Vector2<T> b)
	{
		return Vector2<T>{ a.x / b.x, a.y / b.y };
	}

	template<typename T, typename ScalarT>
	constexpr Vector2<T> operator*(Vector2<T> a, ScalarT s)
	{
		return Vector2<T>{ a.x * s, a.y * s };
	}

	template<typename T, typename ScalarT>
	constexpr Vector2<T> operator*(ScalarT s, Vector2<T> a)
	{
		return Vector2<T>{ a.x * s, a.y * s };
	}

	template<typename T, typename ScalarT>
	constexpr Vector2<T> operator/(Vector2<T> a, ScalarT s)
	{
		return Vector2<T>{ a.x / s, a.y / s };
	}

	template<typename T, typename ScalarT>
	constexpr Vector2<T> operator/(ScalarT s, Vector2<T> a)
	{
		return Vector2<T>{ a.x / s, a.y / s };
	}

	template<typename T1, typename T2>
	constexpr bool operator==(Vector2<T1> a, Vector2<T2> b)
	{
		return a.x == b.x && a.y == b.y;
	}

	template<typename T1, typename T2>
	constexpr bool operator!=(Vector2<T1> a, Vector2<T2> b)
	{
		return a.x != b.x || a.y != b.y;
	}

	template<typename T1, typename T2>
	constexpr bool operator>(Vector2<T1> a, Vector2<T2> b)
	{
		return a.lengthSquared() > b.lengthSquared();
	}

	template<typename T1, typename T2>
	constexpr bool operator<(Vector2<T1> a, Vector2<T2> b)
	{
		return a.lengthSquared() < b.lengthSquared();
	}

    typedef Vector2<int> Vector2i;
    typedef Vector2<unsigned int> Vector2u;
    typedef Vector2<double> Vector2f;
	//typedef Vector2i Point2;

    struct Point2 : public Vector2i
    {
        constexpr Point2() : Vector2i{} { }
        constexpr Point2(int y, int x) : Vector2i(x, y) { }
        constexpr Point2(const Vector2i& v) : Vector2i(v) { }

        constexpr operator const Vector2i&() const
        {
            return *this;
        }

        constexpr operator Vector2i() const
        {
            return Vector2i{x, y};
        }
    };

	template<typename T>
	std::string to_string(Vector2<T> v)
	{
		return "X: " + std::to_string(v.x) + ", Y: " + std::to_string(v.y);
	}
}

