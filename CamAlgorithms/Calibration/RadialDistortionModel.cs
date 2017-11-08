using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace CamAlgorithms.Calibration
{
    // General base for distrotion ( and undistortion ) models
    // It exposes to public only functions to compute R(r), R^-1(r), D(r), U(r) for given p, r and P
    // as well as 1st order derivatives needed to minimise error
    // (all definitions same as in RadialDistortionCorrector)
    // Point p is set to X,Y and
    public abstract class RadialDistortionModel
    {
        public abstract int ParametersCount { get; } // Length of P
        public Vector<double> Coeffs { get; protected set; } // Vector P
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
        
        public abstract void SetInitialParametersFromQuadrics(List<Quadric> quadrics,
            List<List<Vector2>> linePoints, List<int> fitPoints);
    }
}
