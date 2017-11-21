﻿using CamAlgorithms.Calibration;
using System.Windows.Controls;

namespace TriangulationModule
{
    public class Module : CamCore.GuiModule
    {
        private UserControl _moduleControl = null;

        public override string Name { get { return "Triangulation Module"; } }
        public override UserControl MainPanel
        {
            get
            {
                if(_moduleControl == null)
                    _moduleControl = new TraingulationModeTabs();
                return _moduleControl;
            }
        }

        public override bool EndModule()
        {
            return true;
        }

        public override bool StartModule()
        {
            if(!CameraPair.Data.AreCalibrated)
            {
                FailText = "Both cameras need to be calibrated";
                return true;
            }
            return true;
        }
    }
}

