using CamControls;
using CamAlgorithms;
using CamCore;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamAlgorithms.ImageMatching;
using System;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Windows.Input;
using System.IO;
using System.Xml;

namespace ImageMatchingModule
{
    public partial class DisparityMapTab : UserControl
    { 
        public DisparityMap DisparityMapLeft
        {
            get { return _dispControlFirst.Map; }
            set { _dispControlFirst.Map = value; }
        }

        public DisparityMap DisparityMapRight
        {
            get { return _dispControlSec.Map; }
            set { _dispControlSec.Map = value; }
        }

        public DisparityMapTab()
        {
            InitializeComponent();      
        }
    }
}
