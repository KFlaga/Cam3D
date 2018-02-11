namespace CamCore
{
    // Defines generic operation on image pixels with different operations if
    // pixel is on image border
    public static class BorderFunction<T>
    {
        public delegate void FunctionType(T paramObj, int pixRow, int pixCol);

        // Executes 'mainFun' on pixels in range (y:[bh,rows-bh], x:[bw,cols-bw]) and
        // 'borderFun' for rest of pixels (on border)
        public static void DoBorderFunction(T paramObj, FunctionType mainFun, FunctionType borderFun,
            int borderWidth, int borderHeight, int rows, int cols)
        {
            int maxX = cols - borderWidth;
            int maxY = rows - borderHeight;
            for(int y = borderHeight; y < maxY; ++y)
            {
                for(int x = borderWidth; x < maxX; ++x)
                {
                    mainFun(paramObj, y, x);
                }
            }

            // 1) Top border
            for(int y = 0; y < borderHeight; ++y)
                for(int x = 0; x < cols; ++x)
                    borderFun(paramObj, y, x);
            // 2) Right border
            for(int y = borderHeight; y < rows; ++y)
                for(int x = cols - borderWidth; x < cols; ++x)
                    borderFun(paramObj, y, x);
            // 3) Bottom border
            for(int y = rows - borderHeight; y < rows; ++y)
                for(int x = 0; x < maxX; ++x)
                    borderFun(paramObj, y, x);
            // 4) Left border
            for(int y = borderHeight; y < maxY; ++y)
                for(int x = 0; x < borderWidth; ++x)
                    borderFun(paramObj, y, x);
        }
    }
}
