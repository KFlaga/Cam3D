using CamCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
{
    public enum PathDirection
    {
        PosX, NegX, PosY, NegY,
        PosX_PosY, NegX_PosY, PosX_NegY, NegX_NegY,
        PosX2_PosY, NegX2_PosY, PosX2_NegY, NegX2_NegY,
        PosX_PosY2, NegX_PosY2, PosX_NegY2, NegX_NegY2,
    }

    public abstract class Path
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public int Length { get; set; }
        public IntVector2 StartPixel { get; set; }

        public IntVector2 CurrentPixel { get; protected set; } = new IntVector2();
        public int CurrentIndex { get; set; }
        public IntVector2 PreviousPixel { get; protected set; } = new IntVector2();
        public int PreviousIndex { get { return CurrentIndex - 1; } }
        public bool HaveNextPixel
        {
            get { return CurrentIndex < Length - 1; }
        }

        // Need to be allocated externally
        public double[] LastStepCosts { get; set; }

        public abstract void Init();
        public abstract void Next();

        public delegate IntVector2 BorderPixelGetter(IntVector2 basePixel, int rows, int columns);
    }

    public class Path_Str_XPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - StartPixel.X - 1);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            CurrentPixel.X = CurrentPixel.X + 1;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            return new IntVector2(0, pixel.Y);
        }
    }

    public class Path_Str_XNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, StartPixel.X);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            CurrentPixel.X = CurrentPixel.X - 1;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            return new IntVector2(columns - 1, pixel.Y);
        }
    }

    public class Path_Str_YPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageHeight - StartPixel.Y - 1);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y + 1;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            return new IntVector2(pixel.X, 0);
        }
    }

    public class Path_Str_YNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, StartPixel.Y);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y - 1;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            return new IntVector2(pixel.X, rows - 1);
        }
    }

    public class Path_Diag_XPosYPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - StartPixel.X - 1);
            Length = Math.Min(Length, ImageHeight - StartPixel.Y - 1);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X + 1;
            CurrentPixel.Y = CurrentPixel.Y + 1;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            int d = Math.Min(pixel.X, pixel.Y);
            return new IntVector2(pixel.X - d, pixel.Y - d);
        }
    }

    public class Path_Diag_XNegYPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, StartPixel.X);
            Length = Math.Min(Length, ImageHeight - StartPixel.Y - 1);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;
            CurrentPixel.Y = CurrentPixel.Y + 1;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            int d = Math.Min(columns - pixel.X - 1, pixel.Y);
            return new IntVector2(pixel.X + d, pixel.Y - d);
        }
    }

    public class Path_Diag_XPosYNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - StartPixel.X - 1);
            Length = Math.Min(Length, StartPixel.Y);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X + 1;
            CurrentPixel.Y = CurrentPixel.Y - 1;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            int d = Math.Min(pixel.X, rows - pixel.Y - 1);
            return new IntVector2(pixel.X - d, pixel.Y + d);
        }
    }

    public class Path_Diag_XNegYNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, StartPixel.X);
            Length = Math.Min(Length, StartPixel.Y);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;
            CurrentPixel.Y = CurrentPixel.Y - 1;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            int d = Math.Min(columns - pixel.X - 1, rows - pixel.Y - 1);
            return new IntVector2(pixel.X + d, pixel.Y + d);
        }
    }

    // Move 2 steps X left one Y bot
    public class Path_Diag2_X2PosYPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - StartPixel.X - 1);
            Length = Math.Min(Length, (ImageHeight - StartPixel.Y - 1) * 2 + 1);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;

            CurrentPixel.X = CurrentPixel.X + 1;
            CurrentPixel.Y = (CurrentPixel.X & 1) != 0 ?
                CurrentPixel.Y + 1 : CurrentPixel.Y;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            if(pixel.X <= pixel.Y * 2) // Bounded by x
            {
                return new IntVector2(0, pixel.Y - pixel.X / 2);
            }
            else // Bounded by y
            {
                IntVector2 pb = new IntVector2(pixel.X - pixel.Y * 2, 0);
                pb.X = (pixel.X & 1) != 0 ? pb.X - 1 : pb.X;
                return pb;
            }
        }
    }

    public class Path_Diag2_X2NegYPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, StartPixel.X);
            Length = Math.Min(Length, (ImageHeight - StartPixel.Y - 1) * 2 - 1);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;

            CurrentPixel.Y = ((ImageWidth - 1 - CurrentPixel.X) & 1) != 0 ?
                CurrentPixel.Y + 1 : CurrentPixel.Y;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            if(pixel.X <= pixel.Y * 2) // Bounded by x
            {
                return new IntVector2(0, pixel.Y - pixel.X / 2);
            }
            else // Bounded by y
            {
                IntVector2 pb = new IntVector2(pixel.X - pixel.Y * 2, 0);
                pb.X = (pixel.X & 1) != 0 ? pb.X - 1 : pb.X;
                return pb;
            }
            //===================================================//
            if(columns - 1 - pixel.X <= pixel.Y * 2) // Bounded by x
            {
                return new IntVector2(columns - 1, pixel.Y - (columns - 1 - pixel.X) / 2);
            }
            else // Bounded by y
            {
                IntVector2 pb = new IntVector2(pixel.X + pixel.Y * 2, 0);
                pb.X = ((columns - 1 - pixel.X) & 1) != 0 ? pb.X + 1 : pb.X;
                return pb;
            }
        }
    }

    public class Path_Diag2_X2PosYNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - StartPixel.X - 1);
            Length = Math.Min(Length, StartPixel.Y * 2);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X + 1;

            CurrentPixel.Y = (CurrentPixel.X & 1) != 0 ?
                CurrentPixel.Y - 1 : CurrentPixel.Y;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            if(pixel.X <= (rows - 1 - pixel.Y) * 2) // Bounded by x
            {
                return new IntVector2(0, pixel.Y + pixel.X / 2);
            }
            else // Bounded by y
            {
                IntVector2 pb = new IntVector2(pixel.X - (rows - 1 - pixel.Y) * 2, rows - 1);
                pb.X = (pixel.X & 1) != 0 ? pb.X - 1 : pb.X;
                return pb;
            }
        }
    }

    public class Path_Diag2_X2NegYNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, StartPixel.X);
            Length = Math.Min(Length, StartPixel.Y * 2);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;

            CurrentPixel.Y = ((ImageWidth - 1 - CurrentPixel.X) & 1) != 0 ?
                CurrentPixel.Y - 1 : CurrentPixel.Y;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            if(columns - 1 - pixel.X <= (rows - 1 - pixel.Y) * 2) // Bounded by x
            {
                return new IntVector2(columns - 1, pixel.Y + (columns - 1 - pixel.X) / 2);
            }
            else // Bounded by y
            {
                IntVector2 pb = new IntVector2(pixel.X + (rows - 1 - pixel.Y) * 2, rows - 1);
                pb.X = ((columns - 1 - pixel.X) & 1) != 0 ? pb.X + 1 : pb.X;
                return pb;
            }
        }
    }
    
    public class Path_Diag2_XPosY2Pos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, (ImageWidth - StartPixel.X - 1) * 2 - 1);
            Length = Math.Min(Length, ImageHeight - StartPixel.Y - 1);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y + 1;

            CurrentPixel.X = (CurrentPixel.Y & 1) != 0 ?
                CurrentPixel.X + 1 : CurrentPixel.X;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            if(pixel.Y <= pixel.X * 2) // Bounded by y
            {
                return new IntVector2(pixel.X - pixel.Y / 2, 0);
            }
            else // Bounded by x
            {
                IntVector2 pb = new IntVector2(0, pixel.Y - pixel.X * 2);
                pb.Y = ((pixel.Y & 1) & 1) != 0 ? pb.Y - 1 : pb.Y;
                return pb;
            }
        }
    }

    public class Path_Diag2_XNegY2Pos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, StartPixel.X * 2);
            Length = Math.Min(Length, ImageHeight - StartPixel.Y - 1);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y + 1;

            CurrentPixel.X = ((ImageHeight - 1 - CurrentPixel.Y) & 1) != 0 ?
                CurrentPixel.X - 1 : CurrentPixel.X;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            if(pixel.Y <= (columns - 1 - pixel.X) * 2) // Bounded by y
            {
                return new IntVector2(pixel.X + pixel.Y / 2, 0);
            }
            else // Bounded by x
            {
                IntVector2 pb = new IntVector2(columns - 1, pixel.Y - (columns - 1 - pixel.X) * 2);
                pb.Y = ((rows - 1 - pixel.Y) & 1) != 0 ? pb.Y + 1 : pb.Y;
                return pb;
            }
        }
    }

    public class Path_Diag2_XPosY2Neg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, (ImageWidth - StartPixel.X - 1) * 2 - 1);
            Length = Math.Min(Length, StartPixel.Y);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y - 1;

            CurrentPixel.X = (CurrentPixel.Y & 1) != 0 ?
                CurrentPixel.X + 1 : CurrentPixel.X;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            if((rows - 1 - pixel.Y) <= pixel.X * 2) // Bounded by y
            {
                return new IntVector2(pixel.X - (rows - 1 - pixel.Y) / 2, rows - 1);
            }
            else // Bounded by x
            {
                IntVector2 pb = new IntVector2(0, pixel.Y + pixel.X * 2);
                pb.Y = (pixel.Y & 1) != 0 ? pb.Y - 1 : pb.Y;
                return pb;
            }
        }
    }

    public class Path_Diag2_XNegY2Neg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, StartPixel.X * 2);
            Length = Math.Min(Length, StartPixel.Y);

            CurrentPixel.X = StartPixel.X;
            CurrentPixel.Y = StartPixel.Y;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y - 1;

            CurrentPixel.X = ((ImageWidth - 1 - CurrentPixel.X) & 1) != 0 ?
                CurrentPixel.X - 1 : CurrentPixel.X;
        }

        public static IntVector2 GetBorderPixel(IntVector2 pixel, int rows, int columns)
        {
            if((rows - 1 - pixel.Y) <= (columns - 1 - pixel.X) * 2) // Bounded by y
            {
                return new IntVector2(pixel.X + (rows - 1 - pixel.Y) / 2, rows - 1);
            }
            else // Bounded by x
            {
                IntVector2 pb = new IntVector2(columns - 1, pixel.Y + (columns - 1 - pixel.X) * 2);
                pb.Y = ((rows - 1 - pixel.Y) & 1) != 0 ? pb.Y + 1 : pb.Y;
                return pb;
            }
        }
    }
}
