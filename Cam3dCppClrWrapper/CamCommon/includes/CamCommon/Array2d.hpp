#pragma once

#include "PreReqs.hpp"
#include "Vector2.hpp"
#include <vector>
#include <algorithm>

namespace cam3d
{
template<typename T>
class Array2d
{
protected:
    int rows;
    int cols;

    int getIdx(const int r, const int c) const { return r * cols + c; }
    std::vector<T> data;

public:
    Array2d(int rows_, int cols_) : rows{rows_}, cols{cols_}
    {
        data.resize(size());
    }

    T& operator()(const int r, const int c) { return data[getIdx(r, c)]; }
    const T operator()(const int r, const int c) const { return data[getIdx(r, c)]; }

    T& operator()(const Vector2i& pos) { return data[getIdx(pos.y, pos.x)]; }
    const T operator()(const Vector2i& pos) const { return data[getIdx(pos.y, pos.x)]; }

    int getRowCount() const { return rows; }
    int getColumnCount() const { return cols; }
    int size() const { return rows * cols; }

    void clear()
    {
        fill(T{});
    }

    void fill(const T& value)
    {
        for(int i = 0, s = size(); i < s; ++ i)
        {
            data[i] = value;
        }
    }

    auto begin() -> decltype(data.begin())
    {
        return data.begin();
    }

    auto end() -> decltype(data.end())
    {
        return data.begin();
    }
};
}
