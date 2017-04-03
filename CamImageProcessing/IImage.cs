using MathNet.Numerics.LinearAlgebra;
using System;
using System.Windows.Media.Imaging;

namespace CamImageProcessing
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

        void FromBitmapSource(BitmapSource bitmap);
        BitmapSource ToBitmapSource();
    }

    public class ImagesPair
    {
        public IImage Left { get; set; }
        public IImage Right { get; set; }

        public IImage GetImage(CamCore.CameraIndex idx)
        {
            return idx == CamCore.CameraIndex.Left ? Left : Right;
        }

        public void SetImage(CamCore.CameraIndex idx, IImage image)
        {
            if(idx == CamCore.CameraIndex.Left)
                Left = image;
            else
                Right = image;
        }
    }
}
