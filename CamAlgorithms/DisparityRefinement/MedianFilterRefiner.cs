using CamCore;
using System;

namespace CamAlgorithms.ImageMatching
{
    public class MedianFilterRefiner : DisparityRefinement
    {
        public override void RefineMaps()
        {
            if(MapLeft != null)
            {
                MapLeft = FilterMap(MapLeft);
            }

            if(MapRight != null)
            {
                MapRight = FilterMap(MapRight);
            }
        }

        public DisparityMap FilterMap(DisparityMap map)
        {
            DisparityMap filtered = new DisparityMap(map.RowCount, map.ColumnCount);

            Disparity[] window = new Disparity[9];
            int middle = 4;

            int invalidCount = 0;
            Disparity invalidDisparity = new Disparity()
            {
                SubDX = 1e12,
                Flags = (int)DisparityFlags.Invalid
            };
            for(int r = 1; r < map.RowCount - 1; ++r)
            {
                for(int c = 1; c < map.ColumnCount - 1; ++c)
                {
                    int n = 0;
                    invalidCount = 0;
                    for(int y = -1; y <= 1; ++y)
                    {
                        for(int x = -1; x <= 1; ++x)
                        {
                            if((map[r + y, c - 1].Flags & (int)DisparityFlags.Invalid) != 0)
                            {
                                window[n] = invalidDisparity;
                                ++invalidCount;
                            }
                            else
                                window[n] = map[r + y, c - 1];
                            ++n;
                        }
                    }

                    Array.Sort(window, (d1, d2) =>
                    {
                        return d1.SubDX < d2.SubDX ? 1 : d1.SubDX > d2.SubDX ? -1 : 0;
                    });
                    // Set value of image to be median of window
                    filtered.Set(r, c - 1, (Disparity)window[middle + (invalidCount >> 2)].Clone()); // For each 2 invalid cells move middle by 1 pos
                                 // c - 1 to negate some strange horizontal shift
                }
            }

            for(int r = 0; r < map.RowCount; ++r)
            {
                filtered.Set(r, 0, (Disparity)map[r, 0].Clone());
                filtered.Set(r, map.ColumnCount - 2, (Disparity)map[r, map.ColumnCount - 2].Clone());
                filtered.Set(r, map.ColumnCount - 1, (Disparity)map[r, map.ColumnCount - 1].Clone());
            }

            for(int c = 0; c < map.ColumnCount; ++c)
            {
                filtered.Set(0, c, (Disparity)map[0, c].Clone());
                filtered.Set(map.RowCount - 1, c, (Disparity)map[map.RowCount - 1, c].Clone());
            }

            return filtered;
        }
        
        public override string Name
        {
            get
            {
                return "Median Filter";
            }
        }
    }
}
