﻿using CamAlgorithms.Calibration;
using CamCore;
using MathNet.Numerics.LinearAlgebra;

namespace CamAlgorithms
{
    public interface IImageTransformation
    {
        Vector2 TransformPointForwards(Vector2 point);
        Vector2 TransformPointBackwards(Vector2 point);
    }

    public class RadialDistortionTransformation : IImageTransformation
    {
        public RadialDistortionModel DistortionModel { get; set; }

        public RadialDistortionTransformation(RadialDistortionModel model)
        {
            DistortionModel = model;
        }

        public Vector2 TransformPointBackwards(Vector2 point)
        {
            DistortionModel.P = point * DistortionModel.ImageScale;
            DistortionModel.Distort();
            return DistortionModel.Pf / DistortionModel.ImageScale;
        }

        public Vector2 TransformPointForwards(Vector2 point)
        {
            DistortionModel.P = point * DistortionModel.ImageScale;
            DistortionModel.Undistort();
            return DistortionModel.Pf / DistortionModel.ImageScale;
        }
    }

    public class RectificationTransformation : IImageTransformation
    {
        public Matrix<double> RectificationMatrix { get; set; }
        public Matrix<double> RectificationMatrixInverse { get; set; }
        
        public Vector2 TransformPointBackwards(Vector2 point)
        {
            var H = RectificationMatrixInverse;
            double x = H[0, 0] * point.X + H[0, 1] * point.Y + H[0, 2];
            double y = H[1, 0] * point.X + H[1, 1] * point.Y + H[1, 2];
            double w = H[2, 0] * point.X + H[2, 1] * point.Y + H[2, 2];
            return new Vector2(x / w, y / w);
        }

        public Vector2 TransformPointForwards(Vector2 point)
        {
            var H = RectificationMatrix;
            double x = H[0, 0] * point.X + H[0, 1] * point.Y + H[0, 2];
            double y = H[1, 0] * point.X + H[1, 1] * point.Y + H[1, 2];
            double w = H[2, 0] * point.X + H[2, 1] * point.Y + H[2, 2];
            return new Vector2(x / w, y / w);
        }
    }
}
