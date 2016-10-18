using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Collections.Generic;

namespace CamCore
{
    public enum DistortionDirection : int
    {
        None = 0, // No distortion
        ToCenter = 1, // rd/ru < 1 ( rd < ru )
        FromCenter = 2, // rd/ru > 1 ( rd > ru )
        Barrel = ToCenter,
        Cushion = FromCenter,
        Unknown = 3 // Cannot determine distortion
    }

    public class DistortionPoint
    {
        public Vector2 Pi { set; get; } = new Vector2();
        public Vector2 Pd { set; get; } = new Vector2();
        public Vector2 Pu { set; get; } = new Vector2();
        public Vector2 Pf { set; get; } = new Vector2();
        public double Rd { set; get; }
        public double Ru { set; get; }

        public Vector<double> Diff_Xf { set; get; }
        public Vector<double> Diff_Yf { set; get; }

        public Vector<double> Diff_Xu { set; get; }
        public Vector<double> Diff_Yu { set; get; }

        public Vector<double> Diff_Xd { set; get; }
        public Vector<double> Diff_Yd { set; get; }

        public Vector<double> Diff_Ru { set; get; }
        public Vector<double> Diff_Rd { set; get; }

        public DistortionPoint(int modelParametersCount)
        {
            Diff_Xf = new DenseVector(modelParametersCount);
            Diff_Yf = new DenseVector(modelParametersCount);
            Diff_Xu = new DenseVector(modelParametersCount);
            Diff_Yu = new DenseVector(modelParametersCount);
            Diff_Xd = new DenseVector(modelParametersCount);
            Diff_Yd = new DenseVector(modelParametersCount);
            Diff_Ru = new DenseVector(modelParametersCount);
            Diff_Rd = new DenseVector(modelParametersCount);
        }
    }


    // General base for distrotion ( and undistortion ) models
    // It exposes to public only functions to compute R(r), R^-1(r), D(r), U(r) for given p, r and P
    // as well as 1st order derivatives needed to minimise error
    // (all definitions same as in RadialDistortionCorrector)
    // Point p is set to X,Y and
    public abstract class RadialDistortionModel
    {
        public abstract int ParametersCount { get; } // Length of P
        public Vector<double> Parameters { get; protected set; } // Vector P
        public double ImageScale { get; set; } = 1.0; // Scale for image points used in this model : image coords should be scaled by this before supplying P
                                                      // If image is of different size than one used for parameters computation, image coords first needs
                                                      // to be scaled to old ones

        public Vector2 InitialCenterEstimation { get; set; } = new Vector2(); // Initial guess for DC ( probably principal point or image center )
        public double InitialAspectEstimation { get; set; } // Initial guess for Aspect ratio ( probably 1 )
        public bool ComputesAspect { get; protected set; } = false;

        public Vector2 P { get; set; } // measured point
        public Vector2 Pd { get; protected set; } // distorted point in local (distortion) space
        public Vector2 Pu { get; protected set; } // undistorted point in local (distortion) space
        public Vector2 Pf { get; protected set; } // undistorted point in measurements space (final point)

        public double Rd { protected set; get; } // returns rd = sqrt(Xd^2+Yd^2) for given image point (X,Y)
        public double Ru { protected set; get; } // returns ru = R^-1(rd) for given image point (X,Y)

        public Vector<double> Diff_Xf { protected set; get; } // d(xf)/d(P)
        public Vector<double> Diff_Yf { protected set; get; } // d(yf)/d(P)

        public Vector<double> Diff_Xu { protected set; get; } // d(xu)/d(P)
        public Vector<double> Diff_Yu { protected set; get; } // d(yu)/d(P)

        public Vector<double> Diff_Xd { protected set; get; } // d(xu)/d(P)
        public Vector<double> Diff_Yd { protected set; get; } // d(yu)/d(P)

        public Vector<double> Diff_Ru { protected set; get; } // d(R^-1(r))/d(P)
        public Vector<double> Diff_Rd { protected set; get; } // d(rd)/d(P)

        public bool UseNumericDerivative { set; get; } = false;
        public double NumericDerivativeStep { set; get; } = 1e-6;

        public virtual Vector2 DistortionCenter { get; set; }
        public virtual double Aspect { get; set; }

        // Sets initial values for parameters
        public abstract void InitParameters();

        // Computes pd,pu,rd,ru and derivatives after new point (X,Y) or parameter vector is set
        public abstract void FullUpdate();

        public void FullUpdate(DistortionPoint dpoint)
        {
            P = dpoint.Pi;
            FullUpdate();
            dpoint.Pd = new Vector2(Pd);
            dpoint.Pu = new Vector2(Pu);
            dpoint.Pf = new Vector2(Pf);
            dpoint.Rd = Rd;
            dpoint.Ru = Ru;

            Diff_Xd.CopyTo(dpoint.Diff_Xd);
            Diff_Yd.CopyTo(dpoint.Diff_Yd);
            Diff_Xu.CopyTo(dpoint.Diff_Xu);
            Diff_Yu.CopyTo(dpoint.Diff_Yu);
            Diff_Xf.CopyTo(dpoint.Diff_Xf);
            Diff_Yf.CopyTo(dpoint.Diff_Yf);
            Diff_Rd.CopyTo(dpoint.Diff_Rd);
            Diff_Ru.CopyTo(dpoint.Diff_Ru);
        }

        // Computes only (Xf,Yf) for given X,Y and P ( for use after P is computed )
        public abstract void Undistort();

        public void Undistort(DistortionPoint dpoint)
        {
            P = dpoint.Pi;
            Undistort();
            dpoint.Pd = new Vector2(Pd);
            dpoint.Pu = new Vector2(Pu);
            dpoint.Pf = new Vector2(Pf);
            dpoint.Rd = Rd;
            dpoint.Ru = Ru;
        }

        // Distorts point P ( result in Pf )
        public virtual void Distort() { }

        // Distorts point P ( result in Pf )
        public virtual void Distort(DistortionPoint dpoint)
        {
            P = dpoint.Pi;
            Distort();
            dpoint.Pf = new Vector2(Pf);
        }

        // Returns direction of distortion based on distorted points and fitted line between them
        // If ends are further from center than their projection on fit line towards center
        // then we got cushion distortion or barrel otherwise

        public DistortionDirection DirectionFromLine(List<Vector2> line,
            double fitA, double fitB, double fitC)
        {
            // We need to take 2 ends of the line ( points should be sorted )
            Vector2 p1 = line[0];
            Vector2 p2 = line[line.Count - 1];

            return DirectionFromLineEnds(p1, p2, fitA, fitB, fitC);
        }

        public DistortionDirection DirectionFromLine(List<DistortionPoint> line,
            double fitA, double fitB, double fitC)
        {
            // We need to take 2 ends of the line ( points should be sorted )
            Vector2 p1 = line[0].Pf;
            Vector2 p2 = line[line.Count - 1].Pf;

            return DirectionFromLineEnds(p1, p2, fitA, fitB, fitC);
        }

        public DistortionDirection DirectionFromLineEnds(Vector2 p1, Vector2 p2,
            double fitA, double fitB, double fitC)
        {
            // Get projection towards center :
            // p' = p + k(c-p)
            // Ap'x + Bp'y + C = 0
            // A(px + k(cx-px)) + B(py + k(cy-py)) + C = 0
            // k(A(cx-px) + B(cy-py)) = -(Apx + Bpy + C)
            // k = -(Apx + Bpy + C) / (A(cx-px) + B(cy-py))
            // If k > 0  then projection is closer to center than point, if k < 0 then further
            // Also if A(cx-px) + B(cy-py) is zero, then center lies on line, so k = 0
            double k1 = fitA * (DistortionCenter.X - p1.X) + fitB * (DistortionCenter.Y - p1.Y) != 0 ?
                -((fitA * p1.X + fitB * p1.Y + fitC) /
                (fitA * (DistortionCenter.X - p1.X) + fitB * (DistortionCenter.Y - p1.Y))) : 0;

            double k2 = fitA * (DistortionCenter.X - p2.X) + fitB * (DistortionCenter.Y - p2.Y) != 0 ?
                -((fitA * p2.X + fitB * p2.Y + fitC) /
                (fitA * (DistortionCenter.X - p2.X) + fitB * (DistortionCenter.Y - p2.Y))) : 0;

            // If k1 = k2 = 0 we have no distortion
            // If both ends are closer, we have cushion dostortion, of both further - barrel
            // If one is closer, one further its undetermined
            if(k1 == 0.0 && k2 == 0.0)
                return DistortionDirection.None;
            if(k1 >= 0.0 && k2 >= 0.0)
                return DistortionDirection.Cushion;
            if(k1 <= 0.0 && k2 <= 0.0)
                return DistortionDirection.Barrel;

            return DistortionDirection.Unknown;
        }
    }
}
