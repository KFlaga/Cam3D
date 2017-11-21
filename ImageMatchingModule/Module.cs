using System.Windows.Controls;

namespace ImageMatchingModule
{
    public class Module : CamCore.GuiModule
    {
        private ImageMatchingModeTabs _matchingControl = null;

        public override string Name { get { return "Image Matching Module"; } } 
        public override UserControl MainPanel
        {
            get
            {
                if(_matchingControl == null)
                    _matchingControl = new ImageMatchingModeTabs();
                return _matchingControl;
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
                _matchingControl.Dispose();
            }
        }
    }
}
