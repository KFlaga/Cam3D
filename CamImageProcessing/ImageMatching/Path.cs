using CamCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing.ImageMatching
{
    public abstract class Path
    {
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public int Length { get; set; }
        public IntVector2 BasePixel { get; set; }
        public int DisparityRange { get; set; }

        public IntVector2 CurrentPixel { get; protected set; } = new IntVector2();
        public int CurrentIndex { get; set; }
        public IntVector2 PreviousPixel { get; protected set; } = new IntVector2();
        public int PreviousIndex { get { return CurrentIndex - 1; } }
        public bool HaveNextPixel
        {
            get { return CurrentIndex < Length; }
        }

        public double[,] PathCost { get; protected set; }

        public abstract void Init();
        public abstract void Next();
    }

    #region PATHS_NORMAL

    public class Path_Str_XPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X);

            CurrentPixel.X = BasePixel.X - Length;
            CurrentPixel.Y = BasePixel.Y;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = BasePixel.Y;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            CurrentPixel.X = CurrentPixel.X + 1;
        }
    }

    public class Path_Str_XNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - BasePixel.X - 1);

            CurrentPixel.X = BasePixel.X + Length;
            CurrentPixel.Y = BasePixel.Y;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = BasePixel.Y;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            CurrentPixel.X = CurrentPixel.X - 1;
        }
    }

    public class Path_Str_YPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.Y);

            CurrentPixel.X = BasePixel.X;
            CurrentPixel.Y = BasePixel.Y - Length;

            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y + 1;
        }
    }

    public class Path_Str_YNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageHeight - BasePixel.Y - 1);

            CurrentPixel.X = BasePixel.X;
            CurrentPixel.Y = BasePixel.Y + Length;

            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y - 1;
        }
    }

    public class Path_Diag_XPosYPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X);
            Length = Math.Min(Length, BasePixel.Y);

            CurrentPixel.X = BasePixel.X - Length;
            CurrentPixel.Y = BasePixel.Y - Length;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X + 1;
            CurrentPixel.Y = CurrentPixel.Y + 1;
        }
    }

    public class Path_Diag_XNegYPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - BasePixel.X - 1);
            Length = Math.Min(Length, BasePixel.Y);

            CurrentPixel.X = BasePixel.X + Length;
            CurrentPixel.Y = BasePixel.Y - Length;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;
            CurrentPixel.Y = CurrentPixel.Y + 1;
        }
    }

    public class Path_Diag_XPosYNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X);
            Length = Math.Min(Length, ImageHeight - BasePixel.Y - 1);

            CurrentPixel.X = BasePixel.X - Length;
            CurrentPixel.Y = BasePixel.Y + Length;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X + 1;
            CurrentPixel.Y = CurrentPixel.Y - 1;
        }
    }

    public class Path_Diag_XNegYNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - BasePixel.X - 1);
            Length = Math.Min(Length, ImageHeight - BasePixel.Y - 1);

            CurrentPixel.X = BasePixel.X + Length;
            CurrentPixel.Y = BasePixel.Y + Length;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;
            CurrentPixel.Y = CurrentPixel.Y - 1;
        }
    }

    public class Path_Diag2_X2PosYPos : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X);
            Length = Math.Min(Length, BasePixel.Y * 2);

            CurrentPixel.X = BasePixel.X - Length;
            CurrentPixel.Y = BasePixel.Y - (Length + 1) / 2;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X + 1;

            CurrentPixel.Y = _evenStep ?
                CurrentPixel.Y + 1 : CurrentPixel.Y;
            _evenStep = !_evenStep;
        }
    }

    public class Path_Diag2_X2NegYPos : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - BasePixel.X - 1);
            Length = Math.Min(Length, BasePixel.Y * 2);

            CurrentPixel.X = BasePixel.X + Length;
            CurrentPixel.Y = BasePixel.Y - (Length + 1) / 2;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;

            CurrentPixel.Y = _evenStep ?
                CurrentPixel.Y + 1 : CurrentPixel.Y;
            _evenStep = !_evenStep;
        }
    }

    public class Path_Diag2_X2PosYNeg : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X);
            Length = Math.Min(Length, (ImageHeight - BasePixel.Y - 1) * 2 - 1);

            CurrentPixel.X = BasePixel.X - Length;
            CurrentPixel.Y = BasePixel.Y + (Length + 1) / 2;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X + 1;

            CurrentPixel.Y = _evenStep ?
                CurrentPixel.Y - 1 : CurrentPixel.Y;
            _evenStep = !_evenStep;
        }
    }

    public class Path_Diag2_X2NegYNeg : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - BasePixel.X - 1);
            Length = Math.Min(Length, (ImageHeight - BasePixel.Y - 1) * 2 - 1);

            CurrentPixel.X = BasePixel.X + Length;
            CurrentPixel.Y = BasePixel.Y + (Length + 1) / 2;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;

            CurrentPixel.Y = _evenStep ?
                CurrentPixel.Y - 1 : CurrentPixel.Y;
            _evenStep = !_evenStep;
        }
    }


    public class Path_Diag2_XPosY2Pos : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X * 2);
            Length = Math.Min(Length, BasePixel.Y);

            CurrentPixel.X = BasePixel.X - (Length + 1) / 2;
            CurrentPixel.Y = BasePixel.Y - Length;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y + 1;

            CurrentPixel.X = _evenStep ?
                CurrentPixel.X + 1 : CurrentPixel.X;
            _evenStep = !_evenStep;
        }
    }

    public class Path_Diag2_XNegY2Pos : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, (ImageWidth - BasePixel.X - 1)*2 - 1);
            Length = Math.Min(Length, BasePixel.Y);

            CurrentPixel.X = BasePixel.X + (Length + 1) / 2;
            CurrentPixel.Y = BasePixel.Y - Length;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y + 1;

            CurrentPixel.X = _evenStep ?
                CurrentPixel.X - 1 : CurrentPixel.X;
            _evenStep = !_evenStep;
        }
    }

    public class Path_Diag2_XPosY2Neg : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X * 2);
            Length = Math.Min(Length, ImageHeight - BasePixel.Y - 1);

            CurrentPixel.X = BasePixel.X - (Length + 1) / 2;
            CurrentPixel.Y = BasePixel.Y + Length;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y - 1;

            CurrentPixel.X = _evenStep ?
                CurrentPixel.X + 1 : CurrentPixel.X;
            _evenStep = !_evenStep;
        }
    }

    public class Path_Diag2_XNegY2Neg : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, (ImageWidth - BasePixel.X - 1)*2 - 1);
            Length = Math.Min(Length, ImageHeight - BasePixel.Y - 1);

            CurrentPixel.X = BasePixel.X + (Length + 1) / 2;
            CurrentPixel.Y = BasePixel.Y + Length;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y - 1;

            CurrentPixel.X = _evenStep ?
                CurrentPixel.X - 1 : CurrentPixel.X;
            _evenStep = !_evenStep;
        }
    }

    #endregion

    #region PATHS_RECT
    
    public class PathRect_Str_XPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X);

            CurrentPixel.X = BasePixel.X - Length;
            CurrentPixel.Y = BasePixel.Y;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = BasePixel.Y;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            CurrentPixel.X = CurrentPixel.X + 1;
        }
    }

    public class PathRect_Str_XNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - BasePixel.X - 1);

            CurrentPixel.X = BasePixel.X + Length;
            CurrentPixel.Y = BasePixel.Y;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = BasePixel.Y;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            CurrentPixel.X = CurrentPixel.X - 1;
        }
    }

    public class PathRect_Str_YPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.Y);

            CurrentPixel.X = BasePixel.X;
            CurrentPixel.Y = BasePixel.Y - Length;

            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y + 1;
        }
    }

    public class PathRect_Str_YNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageHeight - BasePixel.Y - 1);

            CurrentPixel.X = BasePixel.X;
            CurrentPixel.Y = BasePixel.Y + Length;

            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y - 1;
        }
    }

    public class PathRect_Diag_XPosYPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X);
            Length = Math.Min(Length, BasePixel.Y);

            CurrentPixel.X = BasePixel.X - Length;
            CurrentPixel.Y = BasePixel.Y - Length;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X + 1;
            CurrentPixel.Y = CurrentPixel.Y + 1;
        }
    }

    public class PathRect_Diag_XNegYPos : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - BasePixel.X - 1);
            Length = Math.Min(Length, BasePixel.Y);

            CurrentPixel.X = BasePixel.X + Length;
            CurrentPixel.Y = BasePixel.Y - Length;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;
            CurrentPixel.Y = CurrentPixel.Y + 1;
        }
    }

    public class PathRect_Diag_XPosYNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X);
            Length = Math.Min(Length, ImageHeight - BasePixel.Y - 1);

            CurrentPixel.X = BasePixel.X - Length;
            CurrentPixel.Y = BasePixel.Y + Length;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X + 1;
            CurrentPixel.Y = CurrentPixel.Y - 1;
        }
    }

    public class PathRect_Diag_XNegYNeg : Path
    {
        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - BasePixel.X - 1);
            Length = Math.Min(Length, ImageHeight - BasePixel.Y - 1);

            CurrentPixel.X = BasePixel.X + Length;
            CurrentPixel.Y = BasePixel.Y + Length;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;
            CurrentPixel.Y = CurrentPixel.Y - 1;
        }
    }

    public class PathRect_Diag2_X2PosYPos : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X);
            Length = Math.Min(Length, BasePixel.Y * 2);

            CurrentPixel.X = BasePixel.X - Length;
            CurrentPixel.Y = BasePixel.Y - (Length + 1) / 2;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X + 1;

            CurrentPixel.Y = _evenStep ?
                CurrentPixel.Y + 1 : CurrentPixel.Y;
            _evenStep = !_evenStep;
        }
    }

    public class PathRect_Diag2_X2NegYPos : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - BasePixel.X - 1);
            Length = Math.Min(Length, BasePixel.Y * 2);

            CurrentPixel.X = BasePixel.X + Length;
            CurrentPixel.Y = BasePixel.Y - (Length + 1) / 2;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;

            CurrentPixel.Y = _evenStep ?
                CurrentPixel.Y + 1 : CurrentPixel.Y;
            _evenStep = !_evenStep;
        }
    }

    public class PathRect_Diag2_X2PosYNeg : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X);
            Length = Math.Min(Length, (ImageHeight - BasePixel.Y - 1) * 2 - 1);

            CurrentPixel.X = BasePixel.X - Length;
            CurrentPixel.Y = BasePixel.Y + (Length + 1) / 2;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X + 1;

            CurrentPixel.Y = _evenStep ?
                CurrentPixel.Y - 1 : CurrentPixel.Y;
            _evenStep = !_evenStep;
        }
    }

    public class Path_Diag2_X2NegYNeg : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, ImageWidth - BasePixel.X - 1);
            Length = Math.Min(Length, (ImageHeight - BasePixel.Y - 1) * 2 - 1);

            CurrentPixel.X = BasePixel.X + Length;
            CurrentPixel.Y = BasePixel.Y + (Length + 1) / 2;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.X = CurrentPixel.X - 1;

            CurrentPixel.Y = _evenStep ?
                CurrentPixel.Y - 1 : CurrentPixel.Y;
            _evenStep = !_evenStep;
        }
    }


    public class Path_Diag2_XPosY2Pos : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X * 2);
            Length = Math.Min(Length, BasePixel.Y);

            CurrentPixel.X = BasePixel.X - (Length + 1) / 2;
            CurrentPixel.Y = BasePixel.Y - Length;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y + 1;

            CurrentPixel.X = _evenStep ?
                CurrentPixel.X + 1 : CurrentPixel.X;
            _evenStep = !_evenStep;
        }
    }

    public class Path_Diag2_XNegY2Pos : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, (ImageWidth - BasePixel.X - 1) * 2 - 1);
            Length = Math.Min(Length, BasePixel.Y);

            CurrentPixel.X = BasePixel.X + (Length + 1) / 2;
            CurrentPixel.Y = BasePixel.Y - Length;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y - 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y + 1;

            CurrentPixel.X = _evenStep ?
                CurrentPixel.X - 1 : CurrentPixel.X;
            _evenStep = !_evenStep;
        }
    }

    public class Path_Diag2_XPosY2Neg : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, BasePixel.X * 2);
            Length = Math.Min(Length, ImageHeight - BasePixel.Y - 1);

            CurrentPixel.X = BasePixel.X - (Length + 1) / 2;
            CurrentPixel.Y = BasePixel.Y + Length;

            PreviousPixel.X = CurrentPixel.X - 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y - 1;

            CurrentPixel.X = _evenStep ?
                CurrentPixel.X + 1 : CurrentPixel.X;
            _evenStep = !_evenStep;
        }
    }

    public class Path_Diag2_XNegY2Neg : Path
    {
        bool _evenStep;

        public override void Init()
        {
            CurrentIndex = 0;
            Length = Math.Min(Length, (ImageWidth - BasePixel.X - 1) * 2 - 1);
            Length = Math.Min(Length, ImageHeight - BasePixel.Y - 1);

            CurrentPixel.X = BasePixel.X + (Length + 1) / 2;
            CurrentPixel.Y = BasePixel.Y + Length;

            PreviousPixel.X = CurrentPixel.X + 1;
            PreviousPixel.Y = CurrentPixel.Y + 1;

            PathCost = new double[Length + 1, DisparityRange];
            _evenStep = true;
        }

        public override void Next()
        {
            ++CurrentIndex;
            PreviousPixel.X = CurrentPixel.X;
            PreviousPixel.Y = CurrentPixel.Y;
            CurrentPixel.Y = CurrentPixel.Y - 1;

            CurrentPixel.X = _evenStep ?
                CurrentPixel.X - 1 : CurrentPixel.X;
            _evenStep = !_evenStep;
        }
    }

    #endregion
}
