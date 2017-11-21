using System.Windows.Controls;

namespace RectificationModule
{
    public class Module : CamCore.GuiModule
    {
        private RectificationModeTabs _mainControl = null;

        public override string Name { get { return "Rectification Module"; } } 
        public override UserControl MainPanel
        {
            get
            {
                if(_mainControl == null)
                    _mainControl = new RectificationModeTabs();
                return _mainControl;
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
                _mainControl.Dispose();
            }
        }
    }
}
