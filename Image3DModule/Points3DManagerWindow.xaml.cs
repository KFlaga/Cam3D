﻿using CamCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace Image3DModule
{
    /// <summary>
    /// Interaction logic for Points3DManagerWindow.xaml
    /// </summary>
    public partial class Points3DManagerWindow : Window
    {
        private BindingList<Camera3DPoint> _pointList;
        private List<Camera3DPoint> _savedList;
        public List<Camera3DPoint> Points
        {
            get
            {
                return _savedList;
            }
            set
            {
                _pointList.Clear();
                _savedList.Clear();
                foreach(var point in value)
                {
                    _pointList.Add(point);
                    _savedList.Add(point);
                }
            }
        }

        public Points3DManagerWindow()
        {
            InitializeComponent();
            _savedList = new List<Camera3DPoint>();
            _pointList = new BindingList<Camera3DPoint>();
            _pointListView.ItemsSource = _pointList;
        }

        private void AddPoint(object sender, RoutedEventArgs e)
        {
            Camera3DPoint point = new Camera3DPoint();
            _pointList.Add(point);
        }

        private void DeletePoint(object sender, RoutedEventArgs e)
        {
            if(_pointListView.SelectedIndex != -1)
            {
                if(_pointListView.Items.Count == 1)
                {
                    ClearPointProperties();
                    _pointList.Clear();
                }
                else
                {
                    Camera3DPoint toRemove = (Camera3DPoint)_pointListView.SelectedItem;
                    if(_pointListView.SelectedIndex == 0)
                        _pointListView.SelectedIndex = 1;
                    else
                        _pointListView.SelectedIndex = 0;
                    _pointList.Remove(toRemove);
                }
            }
        }

        private void DeleteAllPoints(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(this, "Confirm clearing of all points",
                "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
            if(result == MessageBoxResult.OK)
            {
                if(_pointListView.Items.Count > 0)
                {
                    ClearPointProperties();
                    _pointList.Clear();
                }
            }
        }

        private void Accept(object sender, RoutedEventArgs e)
        {
            Save();
            DialogResult = true;
            Close();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save()
        {
            _pointListView.Items.Refresh();
            _savedList = _pointList.ToList();
        }

        private void ClearPointProperties()
        {
            BindingOperations.ClearBinding(_tbImgX, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbImgY, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbRealX, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbRealY, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbRealZ, TextBox.TextProperty);
            
            _tbImgX.Text = "";
            _tbImgY.Text = "";
            _tbRealX.Text = "";
            _tbRealY.Text = "";
            _tbRealZ.Text = "";
        }

        private void SelectPoint(object sender, SelectionChangedEventArgs e)
        {
            if(_pointListView.SelectedIndex == -1)
                return;
            ClearPointProperties();
            Camera3DPoint addPoint = (Camera3DPoint)e.AddedItems[0];
            _tbImgX.SetBinding(TextBox.TextProperty, new Binding("X")
            {
                Source = addPoint.Cam1Img,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.DoubleToStringConverter()
            });
            _tbImgY.SetBinding(TextBox.TextProperty, new Binding("Y")
            {
                Source = addPoint.Cam1Img,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.DoubleToStringConverter()
            });
            _tbRealX.SetBinding(TextBox.TextProperty, new Binding("X")
            {
                Source = addPoint.Real,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.DoubleToStringConverter()
            });
            _tbRealY.SetBinding(TextBox.TextProperty, new Binding("Y")
            {
                Source = addPoint.Real,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.DoubleToStringConverter()
            });
            _tbRealZ.SetBinding(TextBox.TextProperty, new Binding("Z")
            {
                Source = addPoint.Real,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.DoubleToStringConverter()
            });
        }

        private void SaveToFile(object sender, RoutedEventArgs e)
        {
            CamCore.FileOperations.SaveToFile(SaveToFile, "Xml File|*.xml");
        }

        private void LoadFromFile(object sender, RoutedEventArgs e)
        {
            CamCore.FileOperations.LoadFromFile(LoadFromFile, "Xml File|*.xml");
        }

        public void LoadFromFile(Stream file, string path)
        {
            XmlDocument dataDoc = new XmlDocument();
            dataDoc.Load(file);

            _pointList.Clear();
            XmlNodeList points = dataDoc.GetElementsByTagName("Point");
            foreach(XmlNode pointNode in points)
            {
                Camera3DPoint point = new Camera3DPoint();
                
                var imgx = pointNode.Attributes["imgx"];
                if(imgx != null)
                    point.Cam1Img.X = double.Parse(imgx.Value);

                var imgy = pointNode.Attributes["imgy"];
                if(imgy != null)
                    point.Cam1Img.Y = double.Parse(imgy.Value);

                var realx = pointNode.Attributes["realx"];
                if(imgy != null)
                    point.Real.X = double.Parse(realx.Value);

                var realy = pointNode.Attributes["realy"];
                if(realy != null)
                    point.Real.Y = double.Parse(realy.Value);

                var realz = pointNode.Attributes["realz"];
                if(realz != null)
                    point.Real.Z = double.Parse(realz.Value);

                _pointList.Add(point);
            }
        }

        public void SaveToFile(Stream file, string path)
        {
            XmlDocument dataDoc = new XmlDocument();
            var rootNode = dataDoc.CreateElement("Points");

            foreach(var point in _pointList)
            {
                var pointNode = dataDoc.CreateElement("Point");

                var attImgX = dataDoc.CreateAttribute("imgx");
                attImgX.Value = point.Cam1Img.X.ToString();
                var attImgY = dataDoc.CreateAttribute("imgy");
                attImgY.Value = point.Cam1Img.Y.ToString();

                var attRealX = dataDoc.CreateAttribute("realx");
                attRealX.Value = point.Real.X.ToString();
                var attRealY = dataDoc.CreateAttribute("realy");
                attRealY.Value = point.Real.Y.ToString();
                var attRealZ = dataDoc.CreateAttribute("realz");
                attRealZ.Value = point.Real.Z.ToString();

                pointNode.Attributes.Append(attImgX);
                pointNode.Attributes.Append(attImgY);
                pointNode.Attributes.Append(attRealX);
                pointNode.Attributes.Append(attRealY);
                pointNode.Attributes.Append(attRealZ);

                rootNode.AppendChild(pointNode);
            }

            dataDoc.InsertAfter(rootNode, dataDoc.DocumentElement);
            dataDoc.Save(file);
        }
    }
}