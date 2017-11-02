using CamAlgorithms;
using System.Windows.Controls;

namespace Image3DModule
{
    public class Image3DConstructionModule : CamCore.Module
    {
        private UserControl _moduleControl = null;

        public override string Name { get { return "Image3D Module"; } } // Name which user sees
        public override UserControl MainPanel
        {
            get
            {
                if (_moduleControl == null)
                    _moduleControl = new Image3DTabs();
                return _moduleControl;
            }
        }

        public override bool EndModule()
        {
            return true;
        }

        public override bool StartModule()
        {
            if (!CalibrationData.Data.IsCamLeftCalibrated ||
                !CalibrationData.Data.IsCamRightCalibrated)
            {
                FailText = "Both cameras need to be calibrated";
                return true;
            }
            return true;
        }
    }
}

