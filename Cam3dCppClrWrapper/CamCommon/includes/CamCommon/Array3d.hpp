#pragma once

#include "PreReqs.hpp"
#include "Vector2.hpp"
#include <vector>
#include <cstring>

namespace cam3d
{
    // Represent static 3d array: [row][col][dim]
    template<typename T>
    class Array3d
    {
    protected:
        int rows;
        int cols;
        int dim;

    private:
        int getSize() const { return rows * cols * dim; }
        int getIdx(const int r, const int c, const int d) const { return (r * cols + c) * dim + d; }
        std::vector<T> data;

    public:
        Array3d(int rows_, int cols_, int dim_) : rows{rows_}, cols{cols_}, dim{dim_}
        {
            data.resize(getSize());
        }

        T& operator()(const int r, const int c, const int d)
        {
            return data[getIdx(r, c, d)];
        }
        const T operator()(const int r, const int c, const int d) const
        {
            return data[getIdx(r, c, d)];
        }
        T& operator()(const Point2 p, const int d)
        {
            return data[getIdx(p.y, p.x, d)];
        }
        const T operator()(const Point2 p, const int d) const
        {
            return data[getIdx(p.y, p.x, d)];
        }

        // Fills array with default objects
        void clear()
        {
            fill(T{});
        }
        // Fills array with given value
        void fill(const T& value)
        {
            for(int i = 0, size = getSize(); i < size; ++i)
            {
                data[i] = value;
            }
        }

        int getRowCount() const { return rows; }
        int getColumnCount() const { return cols; }
        int getDimCount() const { return dim; }

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
