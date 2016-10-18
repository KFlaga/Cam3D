using CamCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;

namespace TriangulationModule
{
    /// <summary>
    /// Interaction logic for CalibrationPointsManagerWindow.xaml
    /// </summary>
    public partial class MatchedPointsManagerWindow : Window
    {
        private BindingList<Vector2> _pointList;
        private List<Vector2> _savedList;
        public List<Vector2> Points 
        {
            get
            {
                return _savedList;
            }
            set
            {
                _pointList.Clear();
                _savedList.Clear();
                foreach (var point in value)
                {
                    _pointList.Add(point);
                    _savedList.Add(point);
                }
            }
        }

        public MatchedPointsManagerWindow()
        {
            InitializeComponent();
            _savedList = new List<Vector2>();
            _pointList = new BindingList<Vector2>();
            _pointListView.ItemsSource = _pointList;
        }

        private void AddPoint(object sender, RoutedEventArgs e)
        {
            Vector2 point = new Vector2();
            _pointList.Add(point);
        }

        private void DeletePoint(object sender, RoutedEventArgs e)
        {
            if (_pointListView.SelectedIndex != -1)
            {
                if (_pointListView.Items.Count == 1)
                {
                    ClearPointProperties();
                    _pointList.Clear();
                }
                else
                {
                    Vector2 toRemove = (Vector2)_pointListView.SelectedItem;
                    if (_pointListView.SelectedIndex == 0)
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
            BindingOperations.ClearBinding(_tbPointIndex, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbImgX, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbImgY, TextBox.TextProperty);
            
            _tbPointIndex.Text = "";
            _tbImgX.Text = "";
            _tbImgY.Text = "";
        }

        private void SelectPoint(object sender, SelectionChangedEventArgs e)
        {
            if (_pointListView.SelectedIndex == -1)
                return;
            ClearPointProperties();
            Vector2 addPoint = (Vector2)e.AddedItems[0];
            _tbPointIndex.SetBinding(TextBox.TextProperty, new Binding("Index")
            {
                Source = addPoint,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.IntToStringConverter()
            });
            _tbImgX.SetBinding(TextBox.TextProperty, new Binding("X")
            {
                Source = addPoint,
                Mode = BindingMode.TwoWay,
                Converter = new CamCore.Converters.DoubleToStringConverter()
            });
            _tbImgY.SetBinding(TextBox.TextProperty, new Binding("Y")
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
                Vector2 point = new Vector2();
                var imgx = pointNode.Attributes["imgx"];
                if(imgx != null)
                    point.X = double.Parse(imgx.Value);

                var imgy = pointNode.Attributes["imgy"];
                if(imgy != null)
                    point.Y = double.Parse(imgy.Value);

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
                attImgX.Value = point.X.ToString();
                var attImgY = dataDoc.CreateAttribute("imgy");
                attImgY.Value = point.Y.ToString();

                pointNode.Attributes.Append(attImgX);
                pointNode.Attributes.Append(attImgY);

                rootNode.AppendChild(pointNode);
            }

            dataDoc.InsertAfter(rootNode, dataDoc.DocumentElement);
            dataDoc.Save(file);
        }
    }
}
