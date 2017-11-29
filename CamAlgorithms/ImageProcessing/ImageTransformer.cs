using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;

namespace CamAlgorithms
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

        public int InterpolationRadius { get; set; } = 1;

        private IntVector2 _finalSize;
        private IntVector2 _finalTopLeft;

        public ImageTransformer(InterpolationMethod method = InterpolationMethod.Linear, int interpolationRadius = 1)
        {
            UsedInterpolationMethod = method;
            InterpolationRadius = interpolationRadius;
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

        struct PatchInfluence
        {
            public double[,] Patch;
            public double Total;
        }

        struct ChannelInfluence
        {
            public double[] Channels;
            public double Total;
        }

        #region FORWARD

        public MaskedImage TransfromImageForwards(IImage image, bool preserveSize = false)
        {
            var influences = FindInfluenceMatrix(image.RowCount, image.ColumnCount);

            if(preserveSize)
            {
                return TransformForwardsPerserveSize(image, influences);
            }
            else
            {
                return TransformForwardsChangeCoords(image, influences);
            }
        }

        private MaskedImage TransformForwardsChangeCoords(IImage image, List<PointInfluence>[,] influences)
        {
            Matrix<double>[] matrices = new Matrix<double>[image.ChannelsCount];
            // New image is in same coords as influence matrix
            for(int i = 0; i < image.ChannelsCount; ++i)
            {
                matrices[i] = new DenseMatrix(_finalSize.Y, _finalSize.X);
            }
            MaskedImage undistorted = new MaskedImage(image.CreateOfSameClass(matrices));

            for(int x = 0; x < _finalSize.X; ++x)
            {
                for(int y = 0; y < _finalSize.Y; ++y)
                {
                    ChannelInfluence cinf = FindInfluenceOfPoint(image, influences[y, x]);
                    SetImageValueBasedOnInfluence(image, undistorted, x, y, cinf, 0.5);
                }
            }

            return undistorted;
        }

        private MaskedImage TransformForwardsPerserveSize(IImage image, List<PointInfluence>[,] influences)
        {
            // New image is in old one's coords
            Matrix<double>[] matrices = new Matrix<double>[image.ChannelsCount];
            for(int i = 0; i < image.ChannelsCount; ++i)
            {
                matrices[i] = new DenseMatrix(image.RowCount, image.ColumnCount);
            }
            MaskedImage undistorted = new MaskedImage(image.CreateOfSameClass(matrices));

            // Bound processing to smaller of images in each dimesions
            int minX = Math.Max(0, _finalTopLeft.X);
            int maxX = Math.Min(image.ColumnCount, _finalSize.X + _finalTopLeft.X);
            int minY = Math.Max(0, _finalTopLeft.Y);
            int maxY = Math.Min(image.RowCount, _finalSize.Y + _finalTopLeft.Y);
            for(int x = minX; x < maxX; ++x)
            {
                for(int y = minY; y < maxY; ++y)
                {
                    ChannelInfluence cinf = FindInfluenceOfPoint(image, influences[y - _finalTopLeft.Y, x - _finalTopLeft.X]);
                    SetImageValueBasedOnInfluence(image, undistorted, x, y, cinf, 0.25);
                }
            }

            return undistorted;
        }


        public List<PointInfluence>[,] FindInfluenceMatrix(int imgRows, int imgCols)
        {
            FindTransformedImageSize(imgRows, imgCols);

            _R = InterpolationRadius;
            _R21 = _R * 2 + 1;

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
                    Vector2 Pu = Transformation.TransformPointForwards(new Vector2(x + 0.5, y + 0.5));
                    IntVector2 PuI = new IntVector2((int)Math.Floor(Pu.X), (int)Math.Floor(Pu.Y));
                    PatchInfluence patch = FindInfluenceInNeighbourhood(Pu, PuI);
                    double infScale = 1.0 / patch.Total;
                    for(int dx = -_R; dx <= _R; ++dx)
                    {
                        for(int dy = -_R; dy <= _R; ++dy)
                        {
                            // Scale influence, so that its sum over all pixels in radius is 1
                            patch.Patch[dx + _R, dy + _R] = patch.Patch[dx + _R, dy + _R] * infScale;
                            // Save influence of Pd on each Pu ( influenceMatrix is in new image coords, 
                            // so Pu is moved to them )
                            influenceMatrix[PuI.Y + dy - _finalTopLeft.Y, PuI.X + dx - _finalTopLeft.X].Add(
                                new PointInfluence(x, y, patch.Patch[dx + _R, dy + _R]));
                        }
                    }
                }
            }

            return influenceMatrix;
        }

        private ChannelInfluence FindInfluenceOfPoint(IImage image, List<PointInfluence> influences)
        {
            double[] channelInfluence = new double[image.ChannelsCount];
            double influenceTotal = 0.0;
            foreach(var inf in influences) // Move Pu to influence matrix coords
            {
                for(int i = 0; i < image.ChannelsCount; ++i)
                {
                    channelInfluence[i] += image[inf.Yd, inf.Xd, i] * inf.Influence;
                }
                influenceTotal += inf.Influence;
            }

            return new ChannelInfluence{ Channels = channelInfluence, Total = influenceTotal };
        }

        private void SetImageValueBasedOnInfluence(IImage image, MaskedImage undistorted, int x, int y, ChannelInfluence cinf, double infTresh)
        {
            if(cinf.Total > infTresh)
            {
                for(int i = 0; i < image.ChannelsCount; ++i)
                {
                    undistorted.GetMatrix(i).At(y, x, cinf.Channels[i] / cinf.Total);
                }
                undistorted.SetMaskAt(y, x, true);
            }
            else
            {
                undistorted.SetMaskAt(y, x, false);
            }
        }

        #endregion FORWARD
        #region BACKWARD

        private int _R, _R21;

        public MaskedImage TransfromImageBackwards(IImage image, bool preserveSize = false)
        {
            FindTransformedImageSize(image.RowCount, image.ColumnCount);
            MaskedImage undistorted = new MaskedImage(image.CreateOfSameClass(CreateMatricesForTransformedImage(image, preserveSize)));

            _R = InterpolationRadius;
            _R21 = _R * 2 + 1;
            for(int x = 0; x < undistorted.ColumnCount; ++x)
            {
                for(int y = 0; y < undistorted.RowCount; ++y)
                {
                    // Cast point from new image to old one
                    Vector2 oldCoords = Transformation.TransformPointBackwards(new Vector2(x: x + 0.5, y: y + 0.5));
                    IntVector2 oldPixel = new IntVector2( x: (int)Math.Floor(oldCoords.X), y : (int)Math.Floor(oldCoords.Y) );
                    if(IsOutOfOldImageOrPointsToPixelWithNoValue(image, oldPixel))
                    {
                        for(int i = 0; i < image.ChannelsCount; ++i)
                        {
                            undistorted.GetMatrix(i).At(y, x, 0.0);
                            undistorted.SetMaskAt(y, x, false);
                        }
                    }
                    else
                    {
                        PatchInfluence patch = FindInfluenceInNeighbourhood(oldCoords, oldPixel);
                        double[] val = FindFinalValueBasedOnInfluence(image, oldPixel, patch);
                        for(int i = 0; i < image.ChannelsCount; ++i)
                        {
                            undistorted.GetMatrix(i).At(y, x, val[i]);
                        }
                    }
                }
            }

            return undistorted;
        }

        private Matrix<double>[] CreateMatricesForTransformedImage(IImage image, bool preserveSize)
        {
            Matrix<double>[] matrices = new Matrix<double>[image.ChannelsCount];
            if(preserveSize)
            {
                for(int i = 0; i < image.ChannelsCount; ++i)
                {
                    matrices[i] = new DenseMatrix(image.RowCount, image.ColumnCount);
                }
            }
            else
            {
                for(int i = 0; i < image.ChannelsCount; ++i)
                {
                    matrices[i] = new DenseMatrix(_finalSize.Y, _finalSize.X);
                }
            }

            return matrices;
        }

        private double[] FindFinalValueBasedOnInfluence(IImage image, IntVector2 oldPixel, PatchInfluence patch)
        {
            double infScale = 1.0 / patch.Total; // Scale influence, so that its sum over all pixels in radius is 1
            double[] val = new double[image.ChannelsCount];
            for(int dx = -_R; dx <= _R; ++dx)
            {
                for(int dy = -_R; dy <= _R; ++dy)
                {
                    double inf = patch.Patch[dx + _R, dy + _R] * infScale;
                    // Store color for new point considering influence from neighbours
                    int ix = Math.Max(0, Math.Min(image.ColumnCount - 1, oldPixel.X + dx));
                    int iy = Math.Max(0, Math.Min(image.RowCount - 1, oldPixel.Y + dy));
                    for(int i = 0; i < image.ChannelsCount; ++i)
                    {
                        val[i] += image[iy, ix, i] * inf;
                    }
                }
            }

            return val;
        }

        private static bool IsOutOfOldImageOrPointsToPixelWithNoValue(IImage image, IntVector2 oldPixel)
        {
            return oldPixel.X < 0 || oldPixel.X >= image.ColumnCount ||
                   oldPixel.Y < 0 || oldPixel.Y >= image.RowCount ||
                   image.HaveValueAt(oldPixel.Y, oldPixel.X) == false;
        }

        #endregion
        
        private PatchInfluence FindInfluenceInNeighbourhood(Vector2 coords, IntVector2 pixel)
        {
            if((coords - new Vector2(0.5, 0.5)).DistanceToSquared(new Vector2(pixel)) < 0.01) // 0.1 pixel distance
            {
                double[,] influence_ = new double[_R21, _R21];
                influence_[_R, _R] = 1.0;
                return new PatchInfluence { Patch = influence_, Total = 1.0 };
            }

            // Interpolate value from patch in old image
            double[,] influence = new double[_R21, _R21];
            double totalInf = 0;
            // For each pixel in neighbourhood find its distance and influence of Pu on it
            for(int dx = -_R; dx <= _R; ++dx)
            {
                for(int dy = -_R; dy <= _R; ++dy)
                {
                    double distance = _computeDistance(coords, pixel.X + dx, pixel.Y + dy);
                    influence[dx + _R, dy + _R] = 1.0 / distance;
                    totalInf += influence[dx + _R, dy + _R];
                }
            }
            return new PatchInfluence { Patch = influence, Total = totalInf };
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
