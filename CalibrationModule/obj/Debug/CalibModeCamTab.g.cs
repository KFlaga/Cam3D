﻿#pragma checksum "..\..\CalibModeCamTab.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "C4164171DAE193D3A240A78D31F4DF21"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using CalibrationModule;
using CamControls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace CalibrationModule {
    
    
    /// <summary>
    /// CalibModeCamTab
    /// </summary>
    public partial class CalibModeCamTab : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 27 "..\..\CalibModeCamTab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button _butAcceptGrid;
        
        #line default
        #line hidden
        
        
        #line 29 "..\..\CalibModeCamTab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal CamControls.IntegerTextBox _textGridNum;
        
        #line default
        #line hidden
        
        
        #line 35 "..\..\CalibModeCamTab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button _butAcceptPoint;
        
        #line default
        #line hidden
        
        
        #line 36 "..\..\CalibModeCamTab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button _butEditPoint;
        
        #line default
        #line hidden
        
        
        #line 38 "..\..\CalibModeCamTab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button _butResetPoints;
        
        #line default
        #line hidden
        
        
        #line 42 "..\..\CalibModeCamTab.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal CamControls.PointImage _imageControl;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/CalibrationModule;component/calibmodecamtab.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\CalibModeCamTab.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 13 "..\..\CalibModeCamTab.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.FindCalibrationPoints);
            
            #line default
            #line hidden
            return;
            case 2:
            
            #line 14 "..\..\CalibModeCamTab.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.ManageGrids);
            
            #line default
            #line hidden
            return;
            case 3:
            
            #line 15 "..\..\CalibModeCamTab.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.ManagePoints);
            
            #line default
            #line hidden
            return;
            case 4:
            
            #line 17 "..\..\CalibModeCamTab.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.ComputeDistortionCorrectionParameters);
            
            #line default
            #line hidden
            return;
            case 5:
            
            #line 18 "..\..\CalibModeCamTab.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.UndistortCalibrationPoints);
            
            #line default
            #line hidden
            return;
            case 6:
            
            #line 19 "..\..\CalibModeCamTab.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.UndistortImage);
            
            #line default
            #line hidden
            return;
            case 7:
            
            #line 20 "..\..\CalibModeCamTab.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Calibrate);
            
            #line default
            #line hidden
            return;
            case 8:
            
            #line 22 "..\..\CalibModeCamTab.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.LoadImageFromMemory);
            
            #line default
            #line hidden
            return;
            case 9:
            this._butAcceptGrid = ((System.Windows.Controls.Button)(target));
            
            #line 27 "..\..\CalibModeCamTab.xaml"
            this._butAcceptGrid.Click += new System.Windows.RoutedEventHandler(this._butAcceptGrid_Click);
            
            #line default
            #line hidden
            return;
            case 10:
            this._textGridNum = ((CamControls.IntegerTextBox)(target));
            return;
            case 11:
            this._butAcceptPoint = ((System.Windows.Controls.Button)(target));
            
            #line 35 "..\..\CalibModeCamTab.xaml"
            this._butAcceptPoint.Click += new System.Windows.RoutedEventHandler(this.AcceptImagePoint);
            
            #line default
            #line hidden
            return;
            case 12:
            this._butEditPoint = ((System.Windows.Controls.Button)(target));
            
            #line 36 "..\..\CalibModeCamTab.xaml"
            this._butEditPoint.Click += new System.Windows.RoutedEventHandler(this._butEditPoint_Click);
            
            #line default
            #line hidden
            return;
            case 13:
            this._butResetPoints = ((System.Windows.Controls.Button)(target));
            
            #line 38 "..\..\CalibModeCamTab.xaml"
            this._butResetPoints.Click += new System.Windows.RoutedEventHandler(this._butResetPoints_Click);
            
            #line default
            #line hidden
            return;
            case 14:
            this._imageControl = ((CamControls.PointImage)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

