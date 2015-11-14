using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DXTestModule
{
    class DXModule : CamCore.Module
    {
        private UserControl _dxControl = null;

        public override string Name { get { return "DX Test Module"; } } // Name which user sees
        public override UserControl MainPanel
        {
            get
            {
                if (_dxControl == null)
                    _dxControl = new DXControl();
                return _dxControl;
            }
        }

        public override bool EndModule()
        {
            // deinit dx
            return true;
        }

        public override bool StartModule()
        {
            // Init dx
            return true;
        }
    }
}
