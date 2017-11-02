using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CamAlgorithms;
using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CamUnitTest
{
    [TestClass]
    public class ImageTransformationTests
    {
        class DummyTransformation : IImageTransformation
        {
            public Matrix<double> Matrix33 { get; set; }
            public Matrix<double> MatrixInverted33 { get; set; }

            public Vector2 TransformPointBackwards(Vector2 point)
            {
                return Transform(point, MatrixInverted33);
            }

            public Vector2 TransformPointForwards(Vector2 point)
            {
                return Transform(point, Matrix33);
            }

            public Vector2 Transform(Vector2 point, Matrix<double> H)
            {
                double x = H[0, 0] * point.X + H[0, 1] * point.Y + H[0, 2];
                double y = H[1, 0] * point.X + H[1, 1] * point.Y + H[1, 2];
                double w = H[2, 0] * point.X + H[2, 1] * point.Y + H[2, 2];
                return new Vector2(x / w, y / w);
            }
        }

        Matrix<double> GetShiftMatrix(int dx, int dy)
        {
            Matrix<double> mat = DenseMatrix.CreateIdentity(3);
            mat[0, 2] = dx;
            mat[1, 2] = dy;
            return mat;
        }

        // Returns test image 10x10 with white 4x4 center and black rest
        IImage GetTestImage()
        {
            IImage img = new GrayScaleImage();
            img.SetMatrix(DenseMatrix.Create(10, 10, 0), 0);
            for(int x = 3; x <= 6; ++x)
            {
                for(int y = 3; y <= 6; ++y)
                {
                    img[y, x] = 1.0;
                }
            }
            return img;
        }

        int _shift = 2;
        double _tolerance = 0.05;

        [TestMethod]
        public void TestForwardTransform_PerserveSize_ShiftPixels()
        {
            // Matrix shifting by (2,2)
            IImage img = GetTestImage();
            Matrix<double> mat = GetShiftMatrix(_shift, _shift);
            Matrix<double> matInvert = GetShiftMatrix(-_shift, -_shift);
            DummyTransformation t = new DummyTransformation { Matrix33 = mat, MatrixInverted33 = matInvert };
            ImageTransformer transformer = new ImageTransformer {
                InterpolationRadius = 1,
                UsedInterpolationMethod = ImageTransformer.InterpolationMethod.Cubic,
                Transformation = t
            };

            MaskedImage result = transformer.TransfromImageForwards(img, true);
            
            CheckShiftTransform(img, result);
        }

        [TestMethod]
        public void TestBackwardsTransform_PerserveSize_ShiftPixels()
        {
            // Matrix shifting by (2,2)
            IImage img = GetTestImage();
            Matrix<double> mat = GetShiftMatrix(_shift, _shift);
            Matrix<double> matInvert = GetShiftMatrix(-_shift, -_shift);
            DummyTransformation t = new DummyTransformation { Matrix33 = mat, MatrixInverted33 = matInvert };
            ImageTransformer transformer = new ImageTransformer
            {
                InterpolationRadius = 1,
                UsedInterpolationMethod = ImageTransformer.InterpolationMethod.Cubic,
                Transformation = t
            };

            MaskedImage result = transformer.TransfromImageBackwards(img, true);

            CheckShiftTransform(img, result);
        }

        private void CheckShiftTransform(IImage img, MaskedImage result)
        {
            // Results : left/top 2 pixels masked, all black except [(5,5),(8,8)] rect (with 5% tolerance)
            Assert.AreEqual(img.RowCount, result.RowCount);
            Assert.AreEqual(img.ColumnCount, result.ColumnCount);

            for(int x = 0; x < _shift; ++x)
            {
                for(int y = 0; y < img.RowCount; ++y)
                {
                    Assert.IsFalse(result.HaveValueAt(y, x));
                }
            }
            for(int x = 0; x < img.ColumnCount; ++x)
            {
                for(int y = 0; y < _shift; ++y)
                {
                    Assert.IsFalse(result.HaveValueAt(y, x));
                }
            }

            for(int x = 0; x < img.ColumnCount - _shift; ++x)
            {
                for(int y = 0; y < img.RowCount - _shift; ++y)
                {
                    Assert.AreEqual(img[y, x], result[y + _shift, x + _shift], _tolerance);
                }
            }
        }
    }
}
