using MathNet.Numerics.LinearAlgebra;
using System;
using System.Windows.Media.Imaging;

namespace CamCore
{
    public interface IImage : ICloneable
    {
        int RowCount { get; }
        int ColumnCount { get; }
        int ChannelsCount { get; }

        double this[int y, int x] { get; set; }
        double this[int y, int x, int channel] { get; set; }

        Matrix<double> GetMatrix(int channel);
        Matrix<double> GetMatrix();
        void SetMatrix(Matrix<double> matrix, int channel);

        bool HaveValueAt(int y, int x);

        new IImage Clone();

        IImage CreateOfSameClass();
        IImage CreateOfSameClass(int rows, int cols);
        IImage CreateOfSameClass(Matrix<double>[] matrices);

        void FromBitmapSource(BitmapSource bitmap);
        BitmapSource ToBitmapSource();
    }

    public class ImagesPair
    {
        public IImage Left { get; set; }
        public IImage Right { get; set; }

        public IImage GetImage(SideIndex idx)
        {
            return idx == SideIndex.Left ? Left : Right;
        }

        public void SetImage(SideIndex idx, IImage image)
        {
            if(idx == SideIndex.Left)
                Left = image;
            else
                Right = image;
        }
    }
}
