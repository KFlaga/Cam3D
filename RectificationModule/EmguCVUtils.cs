using CamCore;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;

namespace RectificationModule
{
    public static class EmguCVUtils
    {
        public static Mat ImageToMat_Gray(CamCore.IImage image)
        {
            Mat mat = new Emgu.CV.Mat(
                image.RowCount, image.ColumnCount, DepthType.Cv8U, 1);

            for(int r = 0; r < image.RowCount; ++r)
            {
                for(int c = 0; c < image.ColumnCount; ++c)
                {
                    mat.SetByteValue(r, c, (byte)(image[r, c] * 255.0));
                }
            }

            return mat;
        }

        public static Mat ImageToMat_Mask(MaskedImage image)
        {
            Mat mat = new Emgu.CV.Mat(
                image.RowCount, image.ColumnCount, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            for(int r = 0; r < image.RowCount; ++r)
            {
                for(int c = 0; c < image.ColumnCount; ++c)
                {
                    byte m = image.HaveValueAt(r, c) ? (byte)255 : (byte)0;
                    mat.SetByteValue(r, c, m);
                }
            }
            return mat;
        }

        public static double GetDoubleValue(this Mat mat, int row, int col)
        {
            var value = new double[1];
            Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1);
            return value[0];
        }

        public static void SetDoubleValue(this Mat mat, int row, int col, double value)
        {
            var target = new[] { value };
            Marshal.Copy(target, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 1);
        }

        public static byte GetByteValue(this Mat mat, int row, int col)
        {
            var value = new byte[1];
            Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1);
            return value[0];
        }

        public static void SetByteValue(this Mat mat, int row, int col, byte value)
        {
            var target = new[] { value };
            Marshal.Copy(target, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 1);
        }
    }
}
