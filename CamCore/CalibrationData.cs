using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using System.IO;
using System.Xml;
using MathNet.Numerics.LinearAlgebra.Single;
using System.ComponentModel;

namespace CamCore
{
    public class CalibrationData : INotifyPropertyChanged
    {
        private static CalibrationData _data = new CalibrationData();
        public static CalibrationData Data { get { return _data; } }

        private Matrix<float> _camLeft = null;
        private Matrix<float> _camRight = null;

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
        public Matrix<float> CameraLeft
        {
            get
            {
                return _camLeft;
            }
            set
            {
                if (value == null)
                {
                    IsCamLeftCalibrated = false;
                    return;
                }
                _camLeft = value;
                IsCamLeftCalibrated = true;
                NotifyPropertyChanged("CameraLeft");

                var RQ = value.SubMatrix(0, 3, 0, 3).QR();
                CalibrationLeft = RQ.R;
                if (Math.Abs(CalibrationLeft[2, 2] - 1) > 0.001)
                {
                    float divCoeff = CalibrationLeft[2, 2];
                    _camLeft = _camLeft.Divide(divCoeff);
                    NotifyPropertyChanged("CameraLeft");
                    RQ = _camLeft.SubMatrix(0, 3, 0, 3).QR();
                    CalibrationLeft = RQ.R;
                }
                RotationLeft = RQ.Q;
                TranslationLeft = value.SubMatrix(0, 3, 0, 3).Inverse().Multiply(value.SubMatrix(0, 3, 3, 1));
                ComputeEssentialFundamental();
            }
        }

        public Matrix<float> CameraRight
        {
            get
            {
                return _camRight;
            }
            set
            {
                if (value == null)
                {
                    IsCamRightCalibrated = false;
                    return;
                }
                _camRight = value;
                IsCamRightCalibrated = true;
                NotifyPropertyChanged("CameraRight");

                var RQ = value.SubMatrix(0, 3, 0, 3).QR();
                CalibrationRight = RQ.R;
                if( Math.Abs(CalibrationRight[2,2] - 1) > 0.001)
                {
                    float divCoeff = CalibrationRight[2, 2];
                    _camRight = _camRight.Divide(divCoeff);
                    NotifyPropertyChanged("CameraRight");
                    RQ = _camRight.SubMatrix(0, 3, 0, 3).QR();
                    CalibrationRight = RQ.R;
                }
                RotationRight = RQ.Q;
                TranslationRight = value.SubMatrix(0, 3, 0, 3).Inverse().Multiply(value.SubMatrix(0, 3, 3, 1));
                ComputeEssentialFundamental();
            }
        }

        #region Additional Matrices

        private Matrix<float> _calibrationLeft;
        public Matrix<float> CalibrationLeft
        {
            get { return _calibrationLeft; }
            private set
            {
                _calibrationLeft = value;
                NotifyPropertyChanged("CalibrationLeft");
            }
        }

        private Matrix<float> _calibrationRight;
        public Matrix<float> CalibrationRight
        {
            get { return _calibrationRight; }
            private set
            {
                _calibrationRight = value;
                NotifyPropertyChanged("CalibrationRight");
            }
        }

        private Matrix<float> _rotationLeft;
        public Matrix<float> RotationLeft
        {
            get { return _rotationLeft; }
            private set
            {
                _rotationLeft = value;
                NotifyPropertyChanged("RotationLeft");
            }
        }

        private Matrix<float> _rotationRight;
        public Matrix<float> RotationRight
        {
            get { return _rotationRight; }
            private set
            {
                _rotationRight = value;
                NotifyPropertyChanged("RotationRight");
            }
        }

        private Matrix<float> _translationLeft;
        public Matrix<float> TranslationLeft
        {
            get { return _translationLeft; }
            private set
            {
                _translationLeft = value;
                NotifyPropertyChanged("TranslationLeft");
            }
        }

        private Matrix<float> _translationRight;
        public Matrix<float> TranslationRight
        {
            get { return _translationRight; }
            private set
            {
                _translationRight = value;
                NotifyPropertyChanged("TranslationRight");
            }
        }

        private Matrix<float> _essential;
        public Matrix<float> Essential
        {
            get { return _essential; }
            private set
            {
                _essential = value;
                NotifyPropertyChanged("Essential");
            }
        }

        private Matrix<float> _fundamental;
        public Matrix<float> Fundamental
        {
            get { return _fundamental; }
            private set
            {
                _fundamental = value;
                NotifyPropertyChanged("Fundamental");
            }
        }

        private void ComputeEssentialFundamental()
        {
            if (!IsCamLeftCalibrated || !IsCamRightCalibrated)
                return;

            // E = [T]xR -> translation/rotation from L to R frames
            // Al = [Rl|Tl], Al^-1 = [Rl^T | -Rl^T * Tl] (https://pl.wikipedia.org/wiki/Elementarne_macierze_transformacji)
            // Al->r = [R|T] = Ar * Al^-1 
            // [R|T] = [Rr*Rl^T | Rr * (-Rl^T * Tl) + Tr]

            Matrix<float> rotLR = RotationRight.Multiply(RotationLeft.Transpose());
            Matrix<float> transLR = (RotationRight.Multiply(
                -RotationLeft.Transpose().Multiply(TranslationLeft)))
                .Add(TranslationRight);

            Matrix<float> skewTransMat = new DenseMatrix(3, 3);
            skewTransMat[0, 0] = 0;
            skewTransMat[0, 1] = -transLR[2, 0];
            skewTransMat[0, 2] = transLR[1, 0];
            skewTransMat[1, 0] = transLR[2, 0];
            skewTransMat[1, 1] = 0;
            skewTransMat[1, 2] = -transLR[0, 0];
            skewTransMat[2, 0] = -transLR[1, 0];
            skewTransMat[2, 1] = transLR[0, 0];
            skewTransMat[2, 2] = 0;

            Essential = skewTransMat.Multiply(rotLR);
            // F = Kr^-T * E * Kl^-1
            Fundamental = CalibrationRight.Inverse().Transpose().Multiply(Essential).Multiply(CalibrationLeft.Inverse());
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion

        #region Save/Load

        public void LoadFromFile(Stream file)
        {
            XmlDocument dataDoc = new XmlDocument();
            dataDoc.Load(file);

            XmlNodeList cameras = dataDoc.GetElementsByTagName("Camera");
            XmlNode cam1 = dataDoc.SelectSingleNode("//Camera[@num='1']");
            if (cam1 == null)
                IsCamLeftCalibrated = false;
            else
            {
                XmlNode is_calib = cam1.Attributes["is_calibrated"];
                if (is_calib == null || !is_calib.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    IsCamLeftCalibrated = false;
                else
                {
                    IsCamLeftCalibrated = true;
                    CameraLeft = ParseCamMatrix(cam1.InnerText);
                }
            }
            XmlNode cam2 = dataDoc.SelectSingleNode("//Camera[@num='2']");
            if (cam2 == null)
                IsCamLeftCalibrated = false;
            else
            {
                XmlNode is_calib = cam2.Attributes["is_calibrated"];
                if (is_calib == null || !is_calib.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    IsCamRightCalibrated = false;
                else
                {
                    IsCamRightCalibrated = true;
                    CameraRight = ParseCamMatrix(cam2.InnerText);
                }
            }
        }

        public void SaveToFile(Stream file)
        {
            XmlDocument dataDoc = new XmlDocument();
            var camerasNode = dataDoc.CreateElement("Cameras");

            var cam1Node = dataDoc.CreateElement("Camera");
            var cam1AttNum = dataDoc.CreateAttribute("num");
            cam1AttNum.Value = "1";
            var cam1AttCalib = dataDoc.CreateAttribute("is_calibrated");
            cam1AttCalib.Value = IsCamLeftCalibrated.ToString();
            cam1Node.Attributes.Append(cam1AttNum);
            cam1Node.Attributes.Append(cam1AttCalib);
            if (IsCamLeftCalibrated)
            {
                cam1Node.InnerText = CamMatrixToString(CameraLeft);
            }
            camerasNode.AppendChild(cam1Node);
            var cam2Node = dataDoc.CreateElement("Camera");
            var cam2AttNum = dataDoc.CreateAttribute("num");
            cam2AttNum.Value = "2";
            var cam2AttCalib = dataDoc.CreateAttribute("is_calibrated");
            cam2AttCalib.Value = IsCamRightCalibrated.ToString();
            cam2Node.Attributes.Append(cam2AttNum);
            cam2Node.Attributes.Append(cam2AttCalib);
            if (IsCamRightCalibrated)
            {
                cam2Node.InnerText = CamMatrixToString(CameraRight);
            }
            camerasNode.AppendChild(cam2Node);

            dataDoc.InsertAfter(camerasNode, dataDoc.DocumentElement);

            dataDoc.Save(file);
        }

        private Matrix<float> ParseCamMatrix(string mat)
        {
            DenseMatrix matrix = new DenseMatrix(3, 4);
            string[] nums = mat.Split('|');
            for (int num = 0; num < 12; num++)
            {
                float val = float.Parse(nums[num]);
                matrix[num / 4, num % 4] = val;
            }
            return matrix;
        }

        private string CamMatrixToString(Matrix<float> matrix)
        {
            StringBuilder nums = new StringBuilder();
            for (int num = 0; num < 12; num++)
            {
                double val = matrix[num / 4, num % 4];
                nums.Append(val);
                nums.Append('|');
            }
            nums.Remove(nums.Length - 1, 1);
            return nums.ToString();
        }

        #endregion
    }
}
