using CamCore;
using System.Collections;
using System.Collections.Generic;
using System;

namespace CalibrationModule
{
    public class CalibrationGrid : IEnumerable<CalibrationShape>
    {
        public int _rowCapacity;
        public int _columnCapacity;

        public int RowCount { get; private set; }
        public int ColumnCount { get; private set; }

        public CalibrationShape[,] ShapesGrid { get; private set; }

        public CalibrationShape this[int y, int x]
        {
            get
            {
                return ShapesGrid[y, x];
            }
            set
            {
                Set(y, x, value);
            }
        }

        public CalibrationGrid()
        {
            _rowCapacity = 0;
            _columnCapacity = 0;
            RowCount = 0;
            ColumnCount = 0;
        }

        public CalibrationGrid(int rowsCap, int colsCap)
        {
            _rowCapacity = rowsCap;
            _columnCapacity = colsCap;

            RowCount = 0;
            ColumnCount = 0;

            ShapesGrid = new CalibrationShape[rowsCap, colsCap];
        }

        // Adds new shape to grid ( position may exceed Row/Cols count )
        public void Add(int y, int x, CalibrationShape shape)
        {
            EnsureCapacity(y+1, x+1);

            if(RowCount <= y)
            {
                RowCount = y+1;
            }
            if(ColumnCount <= x)
            {
                ColumnCount = x+1;
            }

            if(ShapesGrid[y,x] != null)
            {
                ShapesGrid[y, x].GridPos = new TPoint2D<int>(-1, -1);
            }

            ShapesGrid[y, x] = shape;
            ShapesGrid[y, x].GridPos = new TPoint2D<int>(x, y);
        }

        // Adds new shape to grid ( position may exceed Row/Cols count )
        public void Add(TPoint2D<int> p, CalibrationShape shape)
        {
            Add(p.X, p.Y, shape);
        }

        // Set new shape to grid ( position must not exceed Row/Cols count )
        public void Set(int y, int x, CalibrationShape shape)
        {
            if(ShapesGrid[y, x] != null)
            {
                ShapesGrid[y, x].GridPos = new TPoint2D<int>(-1, -1);
            }

            ShapesGrid[y, x] = shape;
            ShapesGrid[y, x].GridPos = new TPoint2D<int>(x, y);
        }

        // Set new shape to grid ( position must not exceed Row/Cols count )
        public void Set(TPoint2D<int> p, CalibrationShape shape)
        {
            Set(p.X, p.Y, shape);
        }

        void EnsureCapacity(int rows, int cols)
        {
            bool capChanged = false;
            while(_rowCapacity <= rows)
            {
                _rowCapacity *= 2;
                capChanged = true;
            }

            while(_columnCapacity <= cols)
            {
                _columnCapacity *= 2;
                capChanged = true;
            }

            if(capChanged)
            {
                var shapesTemp = ShapesGrid;
                ShapesGrid = new CalibrationShape[_rowCapacity, _columnCapacity];

                for(int r = 0; r < RowCount; ++r)
                {
                    for(int c = 0; c < ColumnCount; ++c)
                    {
                        ShapesGrid[r, c] = shapesTemp[r, c];
                    }
                }
            }
        }

        class Enumerator : IEnumerator<CalibrationShape>
        {
            CalibrationGrid _grid;
            int _r, _c;
            public CalibrationShape Current
            {
                get
                {
                    return _grid[_r, _c];
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return _grid[_r, _c];
                }
            }

            public Enumerator(CalibrationGrid grid)
            {
                _grid = grid;
                _r = 0;
                _c = -1;
            }

            public void Dispose()
            {
                _grid = null;
            }

            public bool MoveNext()
            {
                ++_c;
                if( _c >= _grid.ColumnCount )
                {
                    ++_r;
                    if(_r >= _grid.RowCount)
                        return false;
                    _c = 0;
                }
                return true;
            }

            public void Reset()
            {
                _r = 0;
                _c = -1;
            }
        }

        public IEnumerator<CalibrationShape> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }
    }
}
