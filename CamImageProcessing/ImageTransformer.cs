using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;

namespace CamImageProcessing
{
    public class ImageTransformer
    {
        public IImageTransformation Transformation { get; set; }

        public delegate double DistanceComputer(Vector2 Pu, int interpolatedX, int interpolatedY);
        private DistanceComputer _computeDistance;
        public enum InterpolationMethod
        {
            Linear,
            Quadratic,
            Cubic
        }

        InterpolationMethod _intMethod;
        public InterpolationMethod UsedInterpolationMethod
        {
            get { return _intMethod; }
            set
            {
                _intMethod = value;
                switch(value)
                {
                    case InterpolationMethod.Linear:
                        _computeDistance = FindDistance_linear;
                        break;
                    case InterpolationMethod.Quadratic:
                        _computeDistance = FindDistance_quadratic;
                        break;
                    case InterpolationMethod.Cubic:
                        _computeDistance = FindDistance_cubic;
                        break;
                    default:
                        _computeDistance = FindDistance_linear;
                        break;
                }
            }
        }

        int InterpolationRadius = 1;

        private IntVector2 _finalSize;
        private IntVector2 _finalTopLeft;

        public ImageTransformer(InterpolationMethod method = InterpolationMethod.Linear, int r = 1)
        {
            UsedInterpolationMethod = method;
            InterpolationRadius = r;
        }

        #region FORWARD

        public MaskedImage TransfromImageForwards(IImage image, bool preserveSize = false)
        {
            MaskedImage undistorted;
            var influences = FindInfluenceMatrix(image.RowCount, image.ColumnCount);
            Matrix<double>[] matrices = new Matrix<double>[image.ChannelsCount];

            if(preserveSize)
            {
                // New image is in old one's coords
                undistorted = new MaskedImage(image.Clone());
                for(int i = 0; i < image.ChannelsCount; ++i)
                {
                    matrices[i] = new DenseMatrix(image.RowCount, image.ColumnCount);
                    undistorted.SetMatrix(matrices[i], i);
                }

                // Bound processing to smaller of images in each dimesions
                int minX = Math.Max(0, _finalTopLeft.X);
                int maxX = Math.Min(image.ColumnCount, _finalSize.X + _finalTopLeft.X);
                int minY = Math.Max(0, _finalTopLeft.Y);
                int maxY = Math.Min(image.RowCount, _finalSize.Y + _finalTopLeft.Y);
                for(int x = minX; x < maxX; ++x)
                {
                    for(int y = minY; y < maxY; ++y)
                    {
                        double influenceTotal = 0.0;
                        double[] val = new double[image.ChannelsCount];
                        foreach(var inf in influences[y - _finalTopLeft.Y, x - _finalTopLeft.X]) // Move Pu to influence matrix coords
                        {
                            for(int i = 0; i < image.ChannelsCount; ++i)
                                val[i] += image[inf.Yd, inf.Xd, i] * inf.Influence;
                            influenceTotal += inf.Influence;
                        }
                        if(influenceTotal > 0.25)
                        {
                            for(int i = 0; i < image.ChannelsCount; ++i)
                                matrices[i].At(y, x, val[i] / influenceTotal);
                            undistorted.SetMaskAt(y, x, true);
                        }
                        else
                        {
                            undistorted.SetMaskAt(y, x, false);
                        }
                    }
                }
            }
            else
            {
                // New image is in same coords as influence matrix
                for(int i = 0; i < image.ChannelsCount; ++i)
                    matrices[i] = new DenseMatrix(_finalSize.Y, _finalSize.X);
                IImage img = image.Clone();
                for(int i = 0; i < image.ChannelsCount; ++i)
                    img.SetMatrix(matrices[i], i);
                undistorted = new MaskedImage(image.Clone());

                for(int x = 0; x < _finalSize.X; ++x)
                {
                    for(int y = 0; y < _finalSize.Y; ++y)
                    {
                        double influenceTotal = 0.0;
                        double[] val = new double[image.ChannelsCount];
                        foreach(var inf in influences[y, x])
                        {
                            for(int i = 0; i < image.ChannelsCount; ++i)
                                val[i] += image[inf.Yd, inf.Xd, i] * inf.Influence;
                            influenceTotal += inf.Influence;
                        }
                        if(influenceTotal > 0.5)
                        {
                            for(int i = 0; i < image.ChannelsCount; ++i)
                                matrices[i].At(y, x, val[i] / influenceTotal);
                            undistorted.SetMaskAt(y, x, true);
                        }
                        else
                        {
                            undistorted.SetMaskAt(y, x, false);
                        }
                    }
                }
            }

            return undistorted;
        }

        public struct PointInfluence
        {
            public int Xd;
            public int Yd;
            public double Influence;

            public PointInfluence(int px, int py, double i)
            {
                Xd = px;
                Yd = py;
                Influence = i;
            }
        }

        public List<PointInfluence>[,] FindInfluenceMatrix(int imgRows, int imgCols)
        {
            FindTransformedImageSize(imgRows, imgCols);

            int R = InterpolationRadius;
            int R21 = R * 2 + 1;

            List<PointInfluence>[,] influenceMatrix = new List<PointInfluence>[_finalSize.Y, _finalSize.X];
            for(int x = 0; x < _finalSize.X; ++x)
            {
                for(int y = 0; y < _finalSize.Y; ++y)
                {
                    influenceMatrix[y, x] = new List<PointInfluence>();
                }
            }

            // 2) For each Pd find its Pu and influence on final undistorted image  
            for(int x = 0; x < imgCols; ++x)
            {
                for(int y = 0; y < imgRows; ++y)
                {
                    Vector2 Pu = Transformation.TransformPointForwards(new Vector2(x, y));
                    IntVector2 PuI = new IntVector2((int)Pu.X, (int)Pu.Y);

                    double[,] influence = new double[R21, R21];
                    double totalInf = 0;
                    // For each pixel in neighbourhood find its distance and influence of Pu on it
                    for(int dx = -R; dx <= R; ++dx)
                    {
                        for(int dy = -R; dy <= R; ++dy)
                        {
                            double distance = _computeDistance(Pu, PuI.X + dx, PuI.Y + dy);
                            influence[dx + R, dy + R] += 1.0 / distance;
                            totalInf += influence[dx + R, dy + R];
                        }
                    }
                    double infScale = 1.0 / totalInf;
                    for(int dx = -R; dx <= R; ++dx)
                    {
                        for(int dy = -R; dy <= R; ++dy)
                        {
                            // Scale influence, so that its sum over all pixels in radius is 1
                            influence[dx + R, dy + R] = influence[dx + R, dy + R] * infScale;
                            // Save influence of Pd on each Pu ( influenceMatrix is in new image coords, 
                            // so Pu is moved to them )
                            influenceMatrix[PuI.Y + dy - _finalTopLeft.Y, PuI.X + dx - _finalTopLeft.X].Add(
                                new PointInfluence(x, y, influence[dx + R, dy + R]));
                        }
                    }
                }
            }

            return influenceMatrix;
        }

        public void FindTransformedImageSize(int imgRows, int imgCols)
        {
            // 1) Find undistorted size
            // Undistort 4 borders of image and find min/max in each dimension
            double minX = imgCols, minY = imgRows, maxX = 0.0, maxY = 0.0;
            int minXXIdx, minXYIdx, minYXIdx, minYYIdx, maxXXIdx, maxXYIdx, maxYXIdx, maxYYIdx; ;
            for(int x = 0; x < imgCols; ++x)
            {
                Vector2 pf = Transformation.TransformPointForwards(new Vector2(x, 0));
                if(pf.X < minX) { minX = pf.X; minXXIdx = x; minXYIdx = 0; }
                if(pf.Y < minY) { minY = pf.Y; minYXIdx = x; minYYIdx = 0; }
                if(pf.X > maxX) { maxX = pf.X; maxXXIdx = x; maxXYIdx = 0; }
                if(pf.Y > maxY) { maxY = pf.Y; maxYXIdx = x; maxYYIdx = 0; }

                pf = Transformation.TransformPointForwards(new Vector2(x, imgRows - 1));
                if(pf.X < minX) { minX = pf.X; minXXIdx = x; minXYIdx = imgRows - 1; }
                if(pf.Y < minY) { minY = pf.Y; minYXIdx = x; minYYIdx = imgRows - 1; }
                if(pf.X > maxX) { maxX = pf.X; maxXXIdx = x; maxXYIdx = imgRows - 1; }
                if(pf.Y > maxY) { maxY = pf.Y; maxYXIdx = x; maxYYIdx = imgRows - 1; }
            }

            for(int y = 0; y < imgRows; ++y)
            {
                Vector2 pf = Transformation.TransformPointForwards(new Vector2(0, y));
                if(pf.X < minX) { minX = pf.X; minXXIdx = 0; minXYIdx = y; }
                if(pf.Y < minY) { minY = pf.Y; minYXIdx = 0; minYYIdx = y; }
                if(pf.X > maxX) { maxX = pf.X; maxXXIdx = 0; maxXYIdx = y; }
                if(pf.Y > maxY) { maxY = pf.Y; maxYXIdx = 0; maxYYIdx = y; }

                pf = Transformation.TransformPointForwards(new Vector2(imgCols - 1, y));
                if(pf.X < minX) { minX = pf.X; minXXIdx = imgCols - 1; minXYIdx = y; }
                if(pf.Y < minY) { minY = pf.Y; minYXIdx = imgCols - 1; minYYIdx = y; }
                if(pf.X > maxX) { maxX = pf.X; maxXXIdx = imgCols - 1; maxXYIdx = y; }
                if(pf.Y > maxY) { maxY = pf.Y; maxYXIdx = imgCols - 1; maxYYIdx = y; }
            }

            int R = InterpolationRadius;
            _finalTopLeft = new IntVector2((int)minX - R - 1, (int)minY - R - 1); // Position of top-left corner of new image in coords of old one
            _finalSize = new IntVector2(-_finalTopLeft.X + (int)maxX + R + 1, -_finalTopLeft.Y + (int)maxY + R + 1);
        }

        #endregion FORWARD
        #region BACKWARD

        public MaskedImage TransfromImageBackwards(IImage image, bool preserveSize = false)
        {
            MaskedImage undistorted;
            FindTransformedImageSize(image.RowCount, image.ColumnCount);

            Matrix<double>[] matrices = new Matrix<double>[image.ChannelsCount];
            if(preserveSize)
            {
                undistorted = new MaskedImage(image.Clone());
                for(int i = 0; i < image.ChannelsCount; ++i)
                {
                    matrices[i] = new DenseMatrix(image.RowCount, image.ColumnCount);
                    undistorted.SetMatrix(matrices[i], i);
                }
            }
            else
            {
                IImage img = image.Clone();
                for(int i = 0; i < image.ChannelsCount; ++i)
                {
                    matrices[i] = new DenseMatrix(_finalSize.Y, _finalSize.X);
                    img.SetMatrix(matrices[i], i);
                }
                undistorted = new MaskedImage(img.Clone());
            }

            int R = InterpolationRadius;
            int R21 = R * 2 + 1;
            for(int x = 0; x < matrices[0].ColumnCount; ++x)
            {
                for(int y = 0; y < matrices[0].RowCount; ++y)
                {
                    // Cast point from new image to old one
                    Vector2 oldCoords = Transformation.TransformPointBackwards(new Vector2(x: x, y: y));
                    Vector2 aa = Transformation.TransformPointForwards(oldCoords);

                    IntVector2 oldPixel = new IntVector2(oldCoords);
                    // Check if point is in old image range or points to undefined point
                    if(oldCoords.X < 0 || oldCoords.X > image.ColumnCount ||
                        oldCoords.Y < 0 || oldCoords.Y > image.RowCount ||
                        image.HaveValueAt(oldPixel.Y, oldPixel.X) == false)
                    {
                        // Point out of range, so set to black
                        for(int i = 0; i < image.ChannelsCount; ++i)
                        {
                            matrices[i].At(y, x, 0.0);
                            undistorted.SetMaskAt(y, x, false);
                        }
                    }
                    else
                    {
                        // Interpolate value from patch in old image
                        double[,] influence = new double[R21, R21];
                        double totalInf = 0;
                        // For each pixel in neighbourhood find its distance and influence of Pu on it
                        for(int dx = -R; dx <= R; ++dx)
                        {
                            for(int dy = -R; dy <= R; ++dy)
                            {
                                double distance = _computeDistance(oldCoords, oldPixel.X + dx, oldPixel.Y + dy);
                                influence[dx + R, dy + R] = 1.0 / distance;
                                totalInf += influence[dx + R, dy + R];
                            }
                        }
                        double infScale = 1.0 / totalInf; // Scale influence, so that its sum over all pixels in radius is 1
                        double[] val = new double[image.ChannelsCount];
                        for(int dx = -R; dx <= R; ++dx)
                        {
                            for(int dy = -R; dy <= R; ++dy)
                            {
                                double inf = influence[dx + R, dy + R] * infScale;
                                // Store color for new point considering influence from neighbours
                                int ix = Math.Max(0, Math.Min(image.ColumnCount - 1, oldPixel.X + dx));
                                int iy = Math.Max(0, Math.Min(image.RowCount - 1, oldPixel.Y + dy));
                                for(int i = 0; i < image.ChannelsCount; ++i)
                                {
                                    val[i] += image[iy, ix, i] * inf;
                                }
                            }
                        }
                        for(int i = 0; i < image.ChannelsCount; ++i)
                        {
                            matrices[i].At(y, x, val[i]);
                        }
                    }
                }
            }

            return undistorted;
        }

        #endregion

        private double FindDistance_linear(Vector2 point, int px, int py)
        {
            return point.DistanceTo(new Vector2(px + 0.5, py + 0.5));
        }

        private double FindDistance_quadratic(Vector2 point, int px, int py)
        {
            return point.DistanceToSquared(new Vector2(px + 0.5, py + 0.5));
        }

        private double FindDistance_cubic(Vector2 point, int px, int py)
        {
            return Math.Pow(point.DistanceToSquared(new Vector2(px + 0.5, py + 0.5)), 1.5);
        }
    }
}
