using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using System.IO;
using System.Xml;
using MathNet.Numerics.LinearAlgebra.Double;
using System.ComponentModel;

namespace CamCore
{
    public class CalibrationData : INotifyPropertyChanged
    {
        private static CalibrationData _data = new CalibrationData();
        public static CalibrationData Data { get { return _data; } }

        public enum CameraIndex
        {
            Left = 0,
            Right = 1
        }

        private Matrix<double> _camLeft = null;
        private Matrix<double> _camRight = null;

        public bool IsCamLeftCalibrated { get; set; }
        public bool IsCamRightCalibrated { get; set; }

        // Camera matrix M = 
        //  M = KE (E = R[I|-C])
        // Calibration matrix:
        //      |ax  s x0|
        //  K = | 0 ay y0|
        //      | 0  0  1|
        // where:
        //  ax,ay = f*mx,f*my -> m - num of pixels per unit distance, f - focal length, a - pixel dimensions
        // s - skew, x0,y0 - principal point in pixel dimensions
        // Extrinsic params:
        //      |R1 -R1T| (Ri = i-th row of R)
        //  E = |R2 -R2T|
        //      |R3 -R3T|
        // Pcam = R(Pworld − T) (in 3d)
        // pimg = M*Pworld
        public Matrix<double> CameraLeft
        {
            get
            {
                return _camLeft;
            }
            set
            {
                if(value == null)
                {
                    IsCamLeftCalibrated = false;
                    NotifyPropertyChanged("CameraLeft");
                    return;
                }

                _camLeft = value;
                IsCamLeftCalibrated = true;
                UpdateCameraMatrix(_camLeft, CameraIndex.Left);
                NotifyPropertyChanged("CameraLeft");
            }
        }

        public Matrix<double> CameraRight
        {
            get
            {
                return _camRight;
            }
            set
            {
                if(value == null)
                {
                    IsCamRightCalibrated = false;
                    NotifyPropertyChanged("CameraRight");
                    return;
                }

                _camRight = value;
                IsCamRightCalibrated = true;
                UpdateCameraMatrix(_camRight, CameraIndex.Right);
                NotifyPropertyChanged("CameraRight");
            }
        }

        void UpdateCameraMatrix(Matrix<double> camera, CameraIndex index)
        {
            var RQ = camera.SubMatrix(0, 3, 0, 3).QR();

            var calib = RQ.R;
            if(Math.Abs(calib[2, 2] - 1) > 1e-6)
            {
                double scale = calib[2, 2];
                camera.MultiplyThis(scale);
                // NotifyPropertyChanged("CameraLeft");
                RQ = camera.SubMatrix(0, 3, 0, 3).QR();
            }
            calib = RQ.R;
            var rot = RQ.Q;

            // If fx < 0 then set fx = -fx and [r11,r12,r13] = -[r11,r12,r13]
            // As first row of rotation matrix is multiplied only with fx, then changing sign of both
            // fx and this row won't change matrix M = K*R, and so camera matrix (same with fy, but we need to change skew also)
            if(calib[0, 0] < 0)
            {
                calib[0, 0] = -calib[0, 0];
                rot[0, 0] = -rot[0, 0];
                rot[0, 1] = -rot[0, 1];
                rot[0, 2] = -rot[0, 2];
            }
            if(calib[1, 1] < 0)
            {
                calib[1, 1] = -calib[1, 1];
                calib[0, 1] = -calib[0, 1];
                rot[1, 0] = -rot[1, 0];
                rot[1, 1] = -rot[1, 1];
                rot[1, 2] = -rot[1, 2];
            }

            var trans = -camera.SubMatrix(0, 3, 0, 3).Inverse().Multiply(camera.Column(3));

            SetCalibrationMatrix(index, calib);
            SetRotationMatrix(index, rot);
            SetTranslationVector(index, trans);

            ComputeEssentialFundamental();
        }

        public Matrix<double> GetCameraMatrix(CameraIndex idx)
        {
            return idx == CameraIndex.Left ? CameraLeft : CameraRight;
        }

        public void SetCameraMatrix(CameraIndex idx, Matrix<double> matrix)
        {
            if(idx == CameraIndex.Left)
                CameraLeft = matrix;
            else
                CameraRight = matrix;
        }

        #region Additional Matrices

        private Matrix<double> _calibrationLeft;
        public Matrix<double> CalibrationLeft
        {
            get { return _calibrationLeft; }
            set
            {
                _calibrationLeft = value;
                NotifyPropertyChanged("CalibrationLeft");
            }
        }

        private Matrix<double> _calibrationRight;
        public Matrix<double> CalibrationRight
        {
            get { return _calibrationRight; }
            set
            {
                _calibrationRight = value;
                NotifyPropertyChanged("CalibrationRight");
            }
        }

        public Matrix<double> GetCalibrationMatrix(CameraIndex idx)
        {
            return idx == CameraIndex.Left ? CalibrationLeft : CalibrationRight;
        }

        public void SetCalibrationMatrix(CameraIndex idx, Matrix<double> matrix)
        {
            if(idx == CameraIndex.Left)
                CalibrationLeft = matrix;
            else
                CalibrationRight = matrix;
        }

        private Matrix<double> _rotationLeft;
        public Matrix<double> RotationLeft
        {
            get { return _rotationLeft; }
            set
            {
                _rotationLeft = value;
                NotifyPropertyChanged("RotationLeft");
            }
        }

        private Matrix<double> _rotationRight;
        public Matrix<double> RotationRight
        {
            get { return _rotationRight; }
            set
            {
                _rotationRight = value;
                NotifyPropertyChanged("RotationRight");
            }
        }

        public Matrix<double> GetRotationMatrix(CameraIndex idx)
        {
            return idx == CameraIndex.Left ? RotationLeft : RotationRight;
        }

        public void SetRotationMatrix(CameraIndex idx, Matrix<double> matrix)
        {
            if(idx == CameraIndex.Left)
                RotationLeft = matrix;
            else
                RotationRight = matrix;
        }

        private Vector<double> _translationLeft;
        public Vector<double> TranslationLeft
        {
            get { return _translationLeft; }
            set
            {
                _translationLeft = value;
                NotifyPropertyChanged("TranslationLeft");
            }
        }

        private Vector<double> _translationRight;
        public Vector<double> TranslationRight
        {
            get { return _translationRight; }
            set
            {
                _translationRight = value;
                NotifyPropertyChanged("TranslationRight");
            }
        }

        public Vector<double> GetTranslationVector(CameraIndex idx)
        {
            return idx == CameraIndex.Left ? TranslationLeft : TranslationRight;
        }

        public void SetTranslationVector(CameraIndex idx, Vector<double> matrix)
        {
            if(idx == CameraIndex.Left)
                TranslationLeft = matrix;
            else
                TranslationRight = matrix;
        }

        private Matrix<double> _essential;
        public Matrix<double> Essential
        {
            get { return _essential; }
            set
            {
                _essential = value;
                NotifyPropertyChanged("Essential");
            }
        }

        private Matrix<double> _fundamental;
        public Matrix<double> Fundamental
        {
            get { return _fundamental; }
            set
            {
                _fundamental = value;
                NotifyPropertyChanged("Fundamental");
            }
        }

        private Vector<double> _epiPoleLeft;
        public Vector<double> EpipoleLeft
        {
            get { return _epiPoleLeft; }
        }

        private Vector<double> _epiPoleRight;
        public Vector<double> EpipoleRight
        {
            get { return _epiPoleRight; }
        }

        public Vector<double> GetEpipole(CameraIndex idx)
        {
            return idx == CameraIndex.Left ? EpipoleLeft : EpipoleRight;
        }

        private Matrix<double> _epiCrossLeft;
        public Matrix<double> EpipoleCrossLeft
        {
            get { return _epiCrossLeft; }
        }

        private Matrix<double> _epiCrossRight;
        public Matrix<double> EpipoleCrossRight
        {
            get { return _epiCrossRight; }
        }

        private bool _epiLeftInInfinity;
        public bool EpiLeftInInfinity
        {
            get { return _epiLeftInInfinity; }
        }

        private bool _epiRightInInfinity;
        public bool EpiRightInInfinity
        {
            get { return _epiRightInInfinity; }
        }

        private void ComputeEssentialFundamental()
        {
            if(!IsCamLeftCalibrated || !IsCamRightCalibrated)
                return;

            // Find e_R = P_R*C_L, e_L = P_L*C_R
            _epiPoleRight = _camRight * new DenseVector(new double[] { _translationLeft.At(0), _translationLeft.At(1), _translationLeft.At(2), 1.0 });
            _epiPoleLeft = _camLeft * new DenseVector(new double[] { _translationRight.At(0), _translationRight.At(1), _translationRight.At(2), 1.0 });

            _epiLeftInInfinity = false;
            _epiRightInInfinity = false;

            // Check if any epipole is in infinity:
            if(Math.Abs(_epiPoleLeft.At(2)) < 1e-9)
            {
                _epiLeftInInfinity = true;
                // Normalize epipole not by dividing by w, but so |e| = 1
                _epiPoleLeft = _epiPoleLeft.Normalize(2);
            }
            if(Math.Abs(_epiPoleRight.At(2)) < 1e-9)
            {
                _epiRightInInfinity = true;
                // Normalize epipole not by dividing by w, but so |e| = 1
                _epiPoleRight = _epiPoleRight.Normalize(2);
            }


            //             |  0 -e3  e2|
            // Find [e]x = | e3   0 -e1|
            //             |-e2  e1   0|

            _epiCrossRight = new DenseMatrix(3, 3);
            _epiCrossRight[0, 0] = 0.0;
            _epiCrossRight[1, 0] = _epiPoleRight[2];
            _epiCrossRight[2, 0] = -_epiPoleRight[1];
            _epiCrossRight[0, 1] = -_epiPoleRight[2];
            _epiCrossRight[1, 1] = 0.0;
            _epiCrossRight[2, 1] = _epiPoleRight[0];
            _epiCrossRight[0, 2] = _epiPoleRight[1];
            _epiCrossRight[1, 2] = -_epiPoleRight[0];
            _epiCrossRight[2, 2] = 0.0;

            _epiCrossLeft = new DenseMatrix(3, 3);
            _epiCrossLeft[0, 0] = 0.0;
            _epiCrossLeft[1, 0] = _epiPoleLeft[2];
            _epiCrossLeft[2, 0] = -_epiPoleLeft[1];
            _epiCrossLeft[0, 1] = -_epiPoleLeft[2];
            _epiCrossLeft[1, 1] = 0.0;
            _epiCrossLeft[2, 1] = _epiPoleLeft[0];
            _epiCrossLeft[0, 2] = _epiPoleLeft[1];
            _epiCrossLeft[1, 2] = -_epiPoleLeft[0];
            _epiCrossLeft[2, 2] = 0.0;

            // F = [er]x * Pr * pseudoinv(Pl)
            _fundamental = _epiCrossRight * _camRight * (_camLeft.PseudoInverse());
            int rank = _fundamental.Rank();
            if(rank == 3)
            {
                // Need to ensure rank 2, so set smallest singular value to 0
                var svd = _fundamental.Svd();
                var E = svd.W;
                E[2, 2] = 0;
                var oldF = _fundamental;
                _fundamental = svd.U * E * svd.VT;
                var diff = _fundamental - oldF; // Difference should be very small if all is correct

                // NOPE
                // Get new SVD -> last singular value should be 0
                // Vectors corresponding to this value are right/left epipoles (change them to ensure e'^TFe = 0)
               // var evd = _fundamental.Evd();
               // svd = _fundamental.Svd();
            }

            // Scale F, so that F33 = 1
            _fundamental = _fundamental.Divide(_fundamental[2, 2]);

            // E = Kr^T F Kl
            _essential = _calibrationRight.Transpose() * _fundamental * _calibrationLeft;
        }

        #endregion

        private RadialDistortionModel _distortionModelLeft;
        public RadialDistortionModel DistortionModelLeft
        {
            get { return _distortionModelLeft; }
            set
            {
                _distortionModelLeft = value;
                NotifyPropertyChanged("DistortionModelLeft");
            }
        }

        private RadialDistortionModel _distortionModelRight;
        public RadialDistortionModel DistortionModelRight
        {
            get { return _distortionModelRight; }
            set
            {
                _distortionModelRight = value;
                NotifyPropertyChanged("DistortionModelRight");
            }
        }

        public RadialDistortionModel GetDistortionModel(CameraIndex idx)
        {
            return idx == CameraIndex.Left ? DistortionModelLeft : DistortionModelRight;
        }

        public void SetDistortionModel(CameraIndex idx, RadialDistortionModel model)
        {
            if(idx == CameraIndex.Left)
                DistortionModelLeft = model;
            else
                DistortionModelRight = model;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion

        #region Save/Load

        public void LoadFromFile(Stream file, string path)
        {
            XmlDocument dataDoc = new XmlDocument();
            dataDoc.Load(file);

            XmlNodeList cameras = dataDoc.GetElementsByTagName("Camera");
            XmlNode cam1 = dataDoc.SelectSingleNode("//Camera[@num='1']");
            if(cam1 == null)
                IsCamLeftCalibrated = false;
            else
            {
                XmlNode is_calib = cam1.Attributes["is_calibrated"];
                if(is_calib == null || !is_calib.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    IsCamLeftCalibrated = false;
                else
                {
                    IsCamLeftCalibrated = true;
                    CameraLeft = XmlExtensions.MatrixFromNode(cam1.FirstChildWithName("Matrix"));

                    XmlNode modelNode = cam1.FirstChildWithName("DistortionModel");
                    if(modelNode != null)
                        DistortionModelLeft = XmlExtensions.DistortionModelFromNode(modelNode);
                }
            }

            XmlNode cam2 = dataDoc.SelectSingleNode("//Camera[@num='2']");
            if(cam2 == null)
                IsCamLeftCalibrated = false;
            else
            {
                XmlNode is_calib = cam2.Attributes["is_calibrated"];
                if(is_calib == null || !is_calib.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    IsCamRightCalibrated = false;
                else
                {
                    IsCamRightCalibrated = true;
                    CameraRight = XmlExtensions.MatrixFromNode(cam2.FirstChildWithName("Matrix"));

                    XmlNode modelNode = cam2.FirstChildWithName("DistortionModel");
                    if(modelNode != null)
                        DistortionModelRight = XmlExtensions.DistortionModelFromNode(modelNode);
                }
            }
        }

        public void SaveToFile(Stream file, string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            var camerasNode = xmlDoc.CreateElement("Cameras");

            var cam1Node = xmlDoc.CreateElement("Camera");
            var cam1AttNum = xmlDoc.CreateAttribute("num");
            cam1AttNum.Value = "1";
            var cam1AttCalib = xmlDoc.CreateAttribute("is_calibrated");
            cam1AttCalib.Value = IsCamLeftCalibrated.ToString();
            cam1Node.Attributes.Append(cam1AttNum);
            cam1Node.Attributes.Append(cam1AttCalib);

            if(IsCamLeftCalibrated)
            {
                cam1Node.AppendChild(XmlExtensions.CreateMatrixNode(xmlDoc, _camLeft));
                if(_distortionModelLeft != null)
                {
                    cam1Node.AppendChild(XmlExtensions.CreateDistortionModelNode(xmlDoc, _distortionModelLeft));
                }

            }
            camerasNode.AppendChild(cam1Node);

            var cam2Node = xmlDoc.CreateElement("Camera");
            var cam2AttNum = xmlDoc.CreateAttribute("num");
            cam2AttNum.Value = "2";
            var cam2AttCalib = xmlDoc.CreateAttribute("is_calibrated");
            cam2AttCalib.Value = IsCamRightCalibrated.ToString();
            cam2Node.Attributes.Append(cam2AttNum);
            cam2Node.Attributes.Append(cam2AttCalib);

            if(IsCamRightCalibrated)
            {
                cam2Node.AppendChild(XmlExtensions.CreateMatrixNode(xmlDoc, _camRight));
                if(_distortionModelRight != null)
                {
                    cam2Node.AppendChild(XmlExtensions.CreateDistortionModelNode(xmlDoc, _distortionModelRight));
                }
            }
            camerasNode.AppendChild(cam2Node);

            xmlDoc.InsertAfter(camerasNode, xmlDoc.DocumentElement);

            xmlDoc.Save(file);
        }

        #endregion
    }
}
