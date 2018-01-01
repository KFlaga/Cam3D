using CamAlgorithms.Calibration;
using CamCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CamMain
{
    /// <summary>
    /// Interaction logic for CalibrationResults.xaml
    /// </summary>
    public partial class CalibrationResults : UserControl
    {
        CameraPair Cameras { get { return CameraPair.Data; } }
        
        public CalibrationResults()
        {
            InitializeComponent();
        }
        
        
        private void LoadCalibration(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile(
                (stream, path) => { CameraPair.Data.CopyFrom(XmlSerialisation.CreateFromFile<CameraPair>(stream)); },
                "Xml File|*.xml");
            Update();
        }

        private void SaveCalibration(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(
                (stream, path) => { XmlSerialisation.SaveToFile(CameraPair.Data, stream); },
                "Xml File|*.xml");
        }

        private void Update(object sender, RoutedEventArgs e)
        {
            Update();
        }
        
        public void Update()
        {
            _matrixLeftCamera.MatrixSource = Cameras.Left.Matrix;
            _matrixLeftInternal.MatrixSource = Cameras.Left.InternalMatrix;
            _matrixLeftRotation.MatrixSource = RotationConverter.MatrixToEuler(Cameras.Left.RotationMatrix).ToRowMatrix();
            _matrixLeftCenter.MatrixSource = Cameras.Left.Center.ToRowMatrix();
            _matrixRightCamera.MatrixSource = Cameras.Right.Matrix;
            _matrixRightInternal.MatrixSource = Cameras.Right.InternalMatrix;
            _matrixRightRotation.MatrixSource = RotationConverter.MatrixToEuler(Cameras.Right.RotationMatrix).ToRowMatrix();
            _matrixRightCenter.MatrixSource = Cameras.Right.Center.ToRowMatrix();
        }
    }
}
