using System.Windows.Controls;

namespace CalibrationModule
{
    public class CalibModule : CamCore.Module
    {
        private CalibrationModeTabs _calibControl = null;

        public override string Name { get { return "Calibration Module"; } } // Name which user sees
        public override UserControl MainPanel
        {
            get
            {
                if(_calibControl == null)
                {
                    _calibControl = new CalibrationModeTabs();
                }
                return _calibControl;
            }
        }

        public override bool EndModule()
        {
            return true;
        }

        public override bool StartModule()
        {
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                _calibControl.Dispose();
            }
        }
    }
}
