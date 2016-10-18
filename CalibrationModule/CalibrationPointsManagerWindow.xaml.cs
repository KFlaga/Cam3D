using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;

namespace CalibrationModule
{
    /// <summary>
    /// Interaction logic for CalibrationPointsManagerWindow.xaml
    /// </summary>
    public partial class CalibrationPointsManagerWindow : Window
    {
        private BindingList<CalibrationPoint> _pointList;
        private List<CalibrationPoint> _savedList;
        public List<CalibrationPoint> CalibrationPoints
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

        public CalibrationPointsManagerWindow()
        {
            InitializeComponent();
            _savedList = new List<CalibrationPoint>();
            _pointList = new BindingList<CalibrationPoint>();
            _pointListView.ItemsSource = _pointList;
        }

        private void AddPoint(object sender, RoutedEventArgs e)
        {
            CalibrationPoint point = new CalibrationPoint();
            point.GridNum = _pointList.Count;
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
                    CalibrationPoint toRemove = (CalibrationPoint)_pointListView.SelectedItem;
                    if(_pointListView.SelectedIndex == 0)
                        _pointListView.SelectedIndex = 1;
                    else
                        _pointListView.SelectedIndex = 0;
                    _pointList.Remove(toRemove);
                }
                for(int point = 0; point < _pointList.Count; point++)
                {
                    _pointList[point].GridNum = point;
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
            BindingOperations.ClearBinding(_tbCellX, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbCellY, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbGridNum, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbImgX, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbImgY, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbRealX, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbRealY, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbRealZ, TextBox.TextProperty);

            _tbCellX.Text = "";
            _tbGridNum.Text = "";
            _tbCellY.Text = "";
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
            CalibrationPoint addPoint = (CalibrationPoint)e.AddedItems[0];
            _tbGridNum.SetBinding(TextBox.TextProperty, new Binding("GridNum")
            {
                Source = addPoint,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.IntToStringConverter()
            });
            _tbImgX.SetBinding(TextBox.TextProperty, new Binding("ImgX")
            {
                Source = addPoint,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.DoubleToStringConverter()
            });
            _tbImgY.SetBinding(TextBox.TextProperty, new Binding("ImgY")
            {
                Source = addPoint,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.DoubleToStringConverter()
            });
            _tbCellY.SetBinding(TextBox.TextProperty, new Binding("RealRow")
            {
                Source = addPoint,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.IntToStringConverter()
            });
            _tbCellX.SetBinding(TextBox.TextProperty, new Binding("RealCol")
            {
                Source = addPoint,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.IntToStringConverter()
            });
            _tbRealX.SetBinding(TextBox.TextProperty, new Binding("RealX")
            {
                Source = addPoint,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.DoubleToStringConverter()
            });
            _tbRealY.SetBinding(TextBox.TextProperty, new Binding("RealY")
            {
                Source = addPoint,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.DoubleToStringConverter()
            });
            _tbRealZ.SetBinding(TextBox.TextProperty, new Binding("RealZ")
            {
                Source = addPoint,
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
                CalibrationPoint cpoint = new CalibrationPoint();
                var imgx = pointNode.Attributes["imgx"];
                if(imgx != null)
                    cpoint.ImgX = double.Parse(imgx.Value);

                var imgy = pointNode.Attributes["imgy"];
                if(imgy != null)
                    cpoint.ImgY = double.Parse(imgy.Value);

                var gridNum = pointNode.Attributes["grid"];
                if(gridNum != null)
                    cpoint.GridNum = int.Parse(gridNum.Value);

                var col = pointNode.Attributes["gridColumn"];
                if(col != null)
                    cpoint.RealCol = int.Parse(col.Value);

                var row = pointNode.Attributes["gridRow"];
                if(row != null)
                    cpoint.RealRow = int.Parse(row.Value);

                var realx = pointNode.Attributes["realx"];
                if(realx != null)
                    cpoint.RealX = double.Parse(realx.Value);

                var realy = pointNode.Attributes["realy"];
                if(realy != null)
                    cpoint.RealY = double.Parse(realy.Value);

                var realz = pointNode.Attributes["realz"];
                if(realz != null)
                    cpoint.RealZ = double.Parse(realz.Value);

                _pointList.Add(cpoint);
            }
        }

        public void SaveToFile(Stream file, string path)
        {
            XmlDocument dataDoc = new XmlDocument();
            var rootNode = dataDoc.CreateElement("Points");

            foreach(var cpoint in _pointList)
            {
                var pointNode = dataDoc.CreateElement("Point");

                var attImgX = dataDoc.CreateAttribute("imgx");
                attImgX.Value = cpoint.ImgX.ToString("F3");
                var attImgY = dataDoc.CreateAttribute("imgy");
                attImgY.Value = cpoint.ImgY.ToString("F3");
                var attGridNum = dataDoc.CreateAttribute("grid");
                attGridNum.Value = cpoint.GridNum.ToString();
                var attRow = dataDoc.CreateAttribute("gridRow");
                attRow.Value = cpoint.RealRow.ToString();
                var attCol = dataDoc.CreateAttribute("gridColumn");
                attCol.Value = cpoint.RealCol.ToString();
                var attRealX = dataDoc.CreateAttribute("realx");
                attRealX.Value = cpoint.RealX.ToString("F3");
                var attRealY = dataDoc.CreateAttribute("realy");
                attRealY.Value = cpoint.RealY.ToString("F3");
                var attRealZ = dataDoc.CreateAttribute("realz");
                attRealZ.Value = cpoint.RealZ.ToString("F3");

                pointNode.Attributes.Append(attImgX);
                pointNode.Attributes.Append(attImgY);
                pointNode.Attributes.Append(attGridNum);
                pointNode.Attributes.Append(attRow);
                pointNode.Attributes.Append(attCol);
                pointNode.Attributes.Append(attRealX);
                pointNode.Attributes.Append(attRealY);
                pointNode.Attributes.Append(attRealZ);

                rootNode.AppendChild(pointNode);
            }

            dataDoc.InsertAfter(rootNode, dataDoc.DocumentElement);
            dataDoc.Save(file);
        }

        private void TestSet_30(object sender, RoutedEventArgs e)
        {
            List<CalibrationPoint> set30 = new List<CalibrationPoint>();
            // Save every 3rd point
            for(int i = 0; i < _savedList.Count; i = i + 3)
            {
                set30.Add(_savedList[i]);
            }

            CalibrationPoints = set30;
        }

        private void TestSet_31(object sender, RoutedEventArgs e)
        {
            List<CalibrationPoint> set31 = new List<CalibrationPoint>();
            // Save every 3rd point with 1 offset
            for(int i = 1; i < _savedList.Count; i = i + 3)
            {
                set31.Add(_savedList[i]);
            }

            CalibrationPoints = set31;
        }

        private void TestSet_32(object sender, RoutedEventArgs e)
        {
            List<CalibrationPoint> set32 = new List<CalibrationPoint>();
            // Save every 3rd point with 2 offset
            for(int i = 2; i < _savedList.Count; i = i + 3)
            {
                set32.Add(_savedList[i]);
            }

            CalibrationPoints = set32;
        }
    }
}
