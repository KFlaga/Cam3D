﻿#pragma checksum "..\..\PointImage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "262415391140FB13E8B41FCD3382F1F1"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

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


namespace CamControls {
    
    
    /// <summary>
    /// PointImage
    /// </summary>
    public partial class PointImage : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 13 "..\..\PointImage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox _imageMouseXPos;
        
        #line default
        #line hidden
        
        
        #line 15 "..\..\PointImage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox _imageMouseYPos;
        
        #line default
        #line hidden
        
        
        #line 16 "..\..\PointImage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button _butLoadImage;
        
        #line default
        #line hidden
        
        
        #line 19 "..\..\PointImage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox _cbTogglePoints;
        
        #line default
        #line hidden
        
        
        #line 24 "..\..\PointImage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal CamControls.ZoomingScrollControl _zoomControl;
        
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
            System.Uri resourceLocater = new System.Uri("/CamControls;component/pointimage.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\PointImage.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal System.Delegate _CreateDelegate(System.Type delegateType, string handler) {
            return System.Delegate.CreateDelegate(delegateType, this, handler);
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
            this._imageMouseXPos = ((System.Windows.Controls.TextBox)(target));
            return;
            case 2:
            this._imageMouseYPos = ((System.Windows.Controls.TextBox)(target));
            return;
            case 3:
            this._butLoadImage = ((System.Windows.Controls.Button)(target));
            
            #line 16 "..\..\PointImage.xaml"
            this._butLoadImage.Click += new System.Windows.RoutedEventHandler(this.LoadImage);
            
            #line default
            #line hidden
            return;
            case 4:
            this._cbTogglePoints = ((System.Windows.Controls.CheckBox)(target));
            
            #line 19 "..\..\PointImage.xaml"
            this._cbTogglePoints.Checked += new System.Windows.RoutedEventHandler(this._cbTogglePoints_Checked);
            
            #line default
            #line hidden
            
            #line 19 "..\..\PointImage.xaml"
            this._cbTogglePoints.Unchecked += new System.Windows.RoutedEventHandler(this._cbTogglePoints_Unchecked);
            
            #line default
            #line hidden
            return;
            case 5:
            this._zoomControl = ((CamControls.ZoomingScrollControl)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

