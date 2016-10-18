using CamControls;
using CamImageProcessing;
using CamCore;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamImageProcessing.ImageMatching;
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

        private bool _showDX = true;
        public bool IsShownDX
        {
            get { return _showDX; }
            set
            {
                _dispControlFirst.IsShownDX = value;
                _dispControlSec.IsShownDX = value;
                _showDX = value;
            }
        }

        public DisparityMapTab()
        {
            InitializeComponent();      
        }
        
        private void _dispDirectionShowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsShownDX = false;
        }

        private void _dispDirectionShowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsShownDX = true;
        }

        private void SaveLeftMap(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(SaveLeftMap, "Xml File|*.xml");
        }

        private void LoadLeftMap(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile(LoadLeftMap, "Xml File|*.xml");
        }

        private void SaveRightMap(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(SaveRightMap, "Xml File|*.xml");
        }

        private void LoadRightMap(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile(LoadRightMap, "Xml File|*.xml");
        }

        private void SaveLeftMap(Stream file, string path)
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlNode mapNode = DisparityMapLeft.CreateMapNode(xmlDoc);
            xmlDoc.InsertAfter(mapNode, xmlDoc.DocumentElement);

            xmlDoc.Save(file);
        }

        private void LoadLeftMap(Stream file, string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            XmlNode mapNode = xmlDoc.GetElementsByTagName("DisparityMap")[0];
            DisparityMapLeft = DisparityMap.CreateFromNode(mapNode);
        }

        private void SaveRightMap(Stream file, string path)
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlNode mapNode = DisparityMapRight.CreateMapNode(xmlDoc);
            xmlDoc.InsertAfter(mapNode, xmlDoc.DocumentElement);

            xmlDoc.Save(file);
        }

        private void LoadRightMap(Stream file, string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            XmlNode mapNode = xmlDoc.GetElementsByTagName("DisparityMap")[0];
            DisparityMapRight = DisparityMap.CreateFromNode(mapNode);
        }
    }
}

