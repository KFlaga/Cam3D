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

        public enum ProjectionMethod
        {
            Forward,
            Backward
        }

        ProjectionMethod _projMethod;
        public ProjectionMethod UsedProjectionMethod
        {
            get { return _projMethod; }
            set
            {
                _projMethod = value;
                switch(value)
                {
                    case ProjectionMethod.Forward:
                        // _computeDistance = FindDistance_linear;
                        break;
                    case ProjectionMethod.Backward:
                        //_computeDistance = FindDistance_quadratic;
                        break;
                    default:
                        // _computeDistance = FindDistance_linear;
                        break;
                }
            }
        }

        private IntVector2 _finalSize;
        private IntVector2 _finalTopLeft;

        public ImageTransformer(InterpolationMethod method = InterpolationMethod.Linear, int r = 1)
        {
            UsedInterpolationMethod = method;
            InterpolationRadius = r;
        }

        #region FORWARD

        public GrayScaleImage TransfromImageForwards(GrayScaleImage image, bool preserveSize = false)
        {
            GrayScaleImage undistorted = new GrayScaleImage();
            var influences = FindInfluenceMatrix(image.SizeY, image.SizeX);

            Matrix<double> imageMatrix;
            if(preserveSize)
            {
                // New image is in old one's coords
                imageMatrix = new DenseMatrix(image.SizeY, image.SizeX);
                // Bound processing to smaller of images in each dimesions
                int minX = Math.Max(0, _finalTopLeft.X);
                int maxX = Math.Min(image.SizeX, _finalSize.X + _finalTopLeft.X);
                int minY = Math.Max(0, _finalTopLeft.Y);
                int maxY = Math.Min(image.SizeY, _finalSize.Y + _finalTopLeft.Y);
                for(int x = minX; x < maxX; ++x)
                {
                    for(int y = minY; y < maxY; ++y)
                    {
                        double influenceTotal = 0.0;
                        double intensityTotal = 0.0;
                        foreach(var inf in influences[y - _finalTopLeft.Y, x - _finalTopLeft.X]) // Move Pu to influence matrix coords
                        {
                            intensityTotal += image[inf.Yd, inf.Xd] * inf.Influence;
                            influenceTotal += inf.Influence;
                        }
                        imageMatrix[y, x] = intensityTotal / influenceTotal;
                    }
                }
            }
            else
            {
                // New image is in same coords as influence matrix
                imageMatrix = new DenseMatrix(_finalSize.Y, _finalSize.X);
                for(int x = 0; x < _finalSize.X; ++x)
                {
                    for(int y = 0; y < _finalSize.Y; ++y)
                    {
                        double influenceTotal = 0.0;
                        double intensityTotal = 0.0;
                        foreach(var inf in influences[y, x])
                        {
                            intensityTotal += image[inf.Yd, inf.Xd] * inf.Influence;
                            influenceTotal += inf.Influence;
                        }
                        imageMatrix[y, x] = intensityTotal / influenceTotal;
                    }
                }
            }

            undistorted.ImageMatrix = imageMatrix;

            return undistorted;
        }

        public ColorImage TransfromImageForwards(ColorImage image, bool preserveSize = false)
        {
            ColorImage undistorted = image;
            var influences = FindInfluenceMatrix(image.SizeY, image.SizeX);

            Matrix<double> redMatrix, blueMatrix, greenMatrix;
            if(preserveSize)
            {
                // New image is in old one's coords
                redMatrix = new DenseMatrix(image.SizeY, image.SizeX);
                blueMatrix = new DenseMatrix(image.SizeY, image.SizeX);
                greenMatrix = new DenseMatrix(image.SizeY, image.SizeX);
                // Bound processing to smaller of images in each dimesions
                int minX = Math.Max(0, _finalTopLeft.X);
                int maxX = Math.Min(image.SizeX, _finalSize.X + _finalTopLeft.X);
                int minY = Math.Max(0, _finalTopLeft.Y);
                int maxY = Math.Min(image.SizeY, _finalSize.Y + _finalTopLeft.Y);
                for(int x = minX; x < maxX; ++x)
                {
                    for(int y = minY; y < maxY; ++y)
                    {
                        double influenceTotal = 0.0;
                        double red = 0.0, blue = 0.0, green = 0.0;
                        foreach(var inf in influences[y - _finalTopLeft.Y, x - _finalTopLeft.X]) // Move Pu to influence matrix coords
                        {
                            red += image[RGBChannel.Red][inf.Yd, inf.Xd] * inf.Influence;
                            blue += image[RGBChannel.Blue][inf.Yd, inf.Xd] * inf.Influence;
                            green += image[RGBChannel.Green][inf.Yd, inf.Xd] * inf.Influence;
                            influenceTotal += inf.Influence;
                        }
                        redMatrix[y, x] = red / influenceTotal;
                        blueMatrix[y, x] = blue / influenceTotal;
                        greenMatrix[y, x] = green / influenceTotal;
                    }
                }
            }
            else
            {
                // New image is in same coords as influence matrix
                redMatrix = new DenseMatrix(_finalSize.Y, _finalSize.X);
                blueMatrix = new DenseMatrix(_finalSize.Y, _finalSize.X);
                greenMatrix = new DenseMatrix(_finalSize.Y, _finalSize.X);
                for(int x = 0; x < _finalSize.X; ++x)
                {
                    for(int y = 0; y < _finalSize.Y; ++y)
                    {
                        double influenceTotal = 0.0;
                        double red = 0.0, blue = 0.0, green = 0.0;
                        foreach(var inf in influences[y, x])
                        {
                            red += image[RGBChannel.Red][inf.Yd, inf.Xd] * inf.Influence;
                            blue += image[RGBChannel.Blue][inf.Yd, inf.Xd] * inf.Influence;
                            green += image[RGBChannel.Green][inf.Yd, inf.Xd] * inf.Influence;
                            influenceTotal += inf.Influence;
                        }
                        redMatrix.At(y, x, red / influenceTotal);
                        blueMatrix.At(y, x, blue / influenceTotal);
                        greenMatrix.At(y, x, green / influenceTotal);
                    }
                }
            }

            undistorted.ImageMatrix = new Matrix<double>[3] { redMatrix, greenMatrix, blueMatrix };
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

        public GrayScaleImage TransfromImageBackwards(GrayScaleImage image, bool preserveSize = false)
        {
            GrayScaleImage undistorted = new GrayScaleImage();
            FindTransformedImageSize(image.SizeY, image.SizeX);

            Matrix<double> imageMatrix;
            if(preserveSize)
            {
                imageMatrix = new DenseMatrix(image.SizeY, image.SizeX);
            }
            else
            {
                imageMatrix = new DenseMatrix(_finalSize.Y, _finalSize.X);
            }

            int R = InterpolationRadius;
            int R21 = R * 2 + 1;
            for(int x = 0; x < imageMatrix.ColumnCount; ++x)
            {
                for(int y = 0; y < imageMatrix.RowCount; ++y)
                {
                    // Cast point from new image to old one
                    Vector2 oldCoords = Transformation.TransformPointBackwards(new Vector2(y, x));
                    IntVector2 oldPixel = new IntVector2(oldCoords);
                    // Check if point is in old image range
                    if(oldCoords.X < 0 || oldCoords.X > image.SizeX ||
                        oldCoords.Y < 0 || oldCoords.Y > image.SizeY)
                    {
                        // Point out of range, so set to black
                        imageMatrix.At(y, x, 0.0);
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
                        double color = 0.0;
                        for(int dx = -R; dx <= R; ++dx)
                        {
                            for(int dy = -R; dy <= R; ++dy)
                            {
                                // Store color for new point considering influence from neighbours
                                color += image[y, x] * influence[dx + R, dy + R] * infScale;
                            }
                        }
                        imageMatrix.At(y, x, color);
                    }
                }
            }

            undistorted.ImageMatrix = imageMatrix;

            return undistorted;
        }

        public ColorImage TransfromImageBackwards(ColorImage image, bool preserveSize = false)
        {
            ColorImage undistorted = new ColorImage();
            FindTransformedImageSize(image.SizeY, image.SizeX);

            Matrix<double> redMatrix;
            Matrix<double> blueMatrix;
            Matrix<double> greenMatrix;
            if(preserveSize)
            {
                redMatrix = new DenseMatrix(image.SizeY, image.SizeX);
                blueMatrix = new DenseMatrix(image.SizeY, image.SizeX);
                greenMatrix = new DenseMatrix(image.SizeY, image.SizeX);
            }
            else
            {
                redMatrix = new DenseMatrix(_finalSize.Y, _finalSize.X);
                blueMatrix = new DenseMatrix(_finalSize.Y, _finalSize.X);
                greenMatrix = new DenseMatrix(_finalSize.Y, _finalSize.X);
            }

            int R = InterpolationRadius;
            int R21 = R * 2 + 1;
            for(int x = 0; x < redMatrix.ColumnCount; ++x)
            {
                for(int y = 0; y < redMatrix.RowCount; ++y)
                {
                    // Cast point from new image to old one
                    Vector2 oldCoords = Transformation.TransformPointBackwards(new Vector2(x: x, y: y));
                    Vector2 aa = Transformation.TransformPointForwards(oldCoords);

                    IntVector2 oldPixel = new IntVector2(oldCoords);
                    // Check if point is in old image range
                    if(oldCoords.X < 0 || oldCoords.X > image.SizeX ||
                        oldCoords.Y < 0 || oldCoords.Y > image.SizeY)
                    {
                        // Point out of range, so set to black
                        redMatrix.At(y, x, 0.0);
                        blueMatrix.At(y, x, 0.0);
                        greenMatrix.At(y, x, 0.0);
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
                        double red = 0.0, green = 0.0, blue = 0.0;
                        for(int dx = -R; dx <= R; ++dx)
                        {
                            for(int dy = -R; dy <= R; ++dy)
                            {
                                double inf = influence[dx + R, dy + R] * infScale;
                                // Store color for new point considering influence from neighbours
                                int ix = Math.Max(0, Math.Min(image.SizeX - 1, oldPixel.X + dx));
                                int iy = Math.Max(0, Math.Min(image.SizeY - 1, oldPixel.Y + dy));

                                red += image[iy, ix, RGBChannel.Red] * inf;
                                green += image[iy, ix, RGBChannel.Green] * inf;
                                blue += image[iy, ix, RGBChannel.Blue] * inf;
                            }
                        }
                        redMatrix.At(y, x, red);
                        greenMatrix.At(y, x, green);
                        blueMatrix.At(y, x, blue);
                    }
                }
            }

            undistorted[RGBChannel.Red] = redMatrix;
            undistorted[RGBChannel.Green] = greenMatrix;
            undistorted[RGBChannel.Blue] = blueMatrix;

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
