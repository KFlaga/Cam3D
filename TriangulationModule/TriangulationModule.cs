﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TriangulationModule
{
    public class TriModule : CamCore.Module
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
            if(!CamCore.CalibrationData.Data.IsCamLeftCalibrated ||
                !CamCore.CalibrationData.Data.IsCamRightCalibrated)
            {
                FailText = "Both cameras need to be calibrated";
                return true;
            }
            return true;
        }
    }
}
