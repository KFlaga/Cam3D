#pragma once

#include <stdint.h>
#include <limits.h>
#include <string>
#include <cmath>
#include <limits>

#if defined(__x86_64__) || defined(_M_X64)
#define _64bit
#else
#define _32bit
#endif

namespace cam3d
{
#if defined(_64bit)
	typedef int64_t Int;
	typedef uint64_t UInt;
#else
	typedef int32_t Int;
	typedef uint32_t UInt;
#endif

	typedef std::string String;

	inline bool equals(double a, double b, double delta = 1e-6)
	{
		return std::abs(a - b) < delta;
	}
	inline bool notEquals(double a, double b, double delta = 1e-6)
	{
		return std::abs(a - b) > delta;
    }

    namespace detail
    {
    template<typename T, typename... Rest>
    struct multi_min
    {
        static constexpr T getMin(T x, Rest... r)
        {
            return std::min(x, multi_min<Rest...>::getMin(r...));
        }
    };

    template<typename T>
    struct multi_min<T>
    {
        static constexpr T getMin(T x)
        {
            return x;
        }
    };
    }
    template<typename... Args>
    constexpr auto minFrom(Args... args) -> decltype(detail::multi_min<Args...>::getMin(args...))
    {
        return detail::multi_min<Args...>::getMin(args...);
    }

    constexpr int round(const double x)
    {
        return x - static_cast<double>(static_cast<int>(x)) > 0.5 ? static_cast<int>(x) + 1 : static_cast<int>(x);
    }
}
