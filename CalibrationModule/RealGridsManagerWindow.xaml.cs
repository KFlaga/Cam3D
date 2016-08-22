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
    /// Interaction logic for RealGridsManagerWindow.xaml
    /// </summary>
    public partial class RealGridsManagerWindow : Window
    {
        private BindingList<RealGridData> _gridsList;
        private List<RealGridData> _savedList;
        public List<RealGridData> RealGrids 
        {
            get
            {
                return _savedList;
            }
            set
            {
                _gridsList.Clear();
                _savedList.Clear();
                foreach (var rg in value)
                {
                    _gridsList.Add(rg);
                    _savedList.Add(rg);
                }
            }
        }

        public RealGridsManagerWindow()
        {
            InitializeComponent();
            _savedList = new List<RealGridData>();
            _gridsList = new BindingList<RealGridData>();
            _gridListView.ItemsSource = _gridsList;
        }

        private void AddGrid(object sender, RoutedEventArgs e)
        {
            RealGridData rgrid = new RealGridData();
            rgrid.Num = _gridsList.Count;
            _gridsList.Add(rgrid);

            //ListBoxItem item = new ListBoxItem();
            //item.Content = rgrid;
            //_gridListView.Items.Add(item);
        }

        private void DeleteGrid(object sender, RoutedEventArgs e)
        {
            if (_gridListView.SelectedIndex != -1)
            {
                if (_gridListView.Items.Count == 1)
                {
                    ClearGridProperties();
                    _gridsList.Clear();
                }
                else
                {
                    RealGridData toRemove = (RealGridData)_gridListView.SelectedItem;
                    if(_gridListView.SelectedIndex == 0)
                        _gridListView.SelectedIndex = 1;
                    else
                        _gridListView.SelectedIndex = 0;
                    _gridsList.Remove(toRemove);   
                }
                for(int grid = 0; grid < _gridsList.Count; grid++)
                {
                    _gridsList[grid].Num = grid;
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
            _gridListView.Items.Refresh();
            _savedList = _gridsList.ToList();
        }

        private void ClearGridProperties()
        {
            BindingOperations.ClearBinding(_tbGridNum, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbLabel, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbWidthX, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbWidthY, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbWidthZ, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbHeightX, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbHeightY, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbHeightZ, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbZeroY, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbZeroZ, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbCols, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbRows, TextBox.TextProperty);

            _tbCols.Text = "";
            _tbGridNum.Text = "";
            _tbLabel.Text = "";
            _tbRows.Text = "";
            _tbZeroX.Text = "";
            _tbZeroY.Text = "";
            _tbZeroZ.Text = "";
            _tbHeightX.Text = "";
            _tbHeightY.Text = "";
            _tbHeightZ.Text = "";
            _tbWidthX.Text = "";
            _tbWidthY.Text = "";
            _tbWidthZ.Text = "";
        }

        private void SelectGrid(object sender, SelectionChangedEventArgs e)
        {
            if(e.RemovedItems.Count == 1)
            {
                ClearGridProperties();
            }
            if (e.AddedItems.Count == 1)
            {
                RealGridData addgrid = (RealGridData)e.AddedItems[0];
                _tbGridNum.SetBinding(TextBox.TextProperty, new Binding("Num")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay
                });
                _tbLabel.SetBinding(TextBox.TextProperty, new Binding("Label")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay
                });
                _tbWidthX.SetBinding(TextBox.TextProperty, new Binding("WidthX")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay,
                    Converter = new CamCore.Converters.DoubleToStringConverter()
                });
                _tbWidthY.SetBinding(TextBox.TextProperty, new Binding("WidthY")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay,
                    Converter = new CamCore.Converters.DoubleToStringConverter()
                });
                _tbWidthZ.SetBinding(TextBox.TextProperty, new Binding("WidthZ")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay,
                    Converter = new CamCore.Converters.DoubleToStringConverter()
                });
                _tbHeightX.SetBinding(TextBox.TextProperty, new Binding("HeightX")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay,
                    Converter = new CamCore.Converters.DoubleToStringConverter()
                });
                _tbHeightY.SetBinding(TextBox.TextProperty, new Binding("HeightY")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay,
                    Converter = new CamCore.Converters.DoubleToStringConverter()
                });
                _tbHeightZ.SetBinding(TextBox.TextProperty, new Binding("HeightZ")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay,
                    Converter = new CamCore.Converters.DoubleToStringConverter()
                });
                _tbZeroX.SetBinding(TextBox.TextProperty, new Binding("ZeroX")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay,
                    Converter = new CamCore.Converters.DoubleToStringConverter()
                });
                _tbZeroY.SetBinding(TextBox.TextProperty, new Binding("ZeroY")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay,
                    Converter = new CamCore.Converters.DoubleToStringConverter()
                });
                _tbZeroZ.SetBinding(TextBox.TextProperty, new Binding("ZeroZ")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay,
                    Converter = new CamCore.Converters.DoubleToStringConverter()
                });
                _tbCols.SetBinding(TextBox.TextProperty, new Binding("Cols")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay,
                    Converter = new CamCore.Converters.IntToStringConverter()
                });
                _tbRows.SetBinding(TextBox.TextProperty, new Binding("Rows")
                {
                    Source = addgrid,
                    Mode = BindingMode.TwoWay,
                    Converter = new CamCore.Converters.IntToStringConverter()
                });
            }
        }

        public void SaveToFile(object sender, RoutedEventArgs e)
        {
            CamCore.FileOperations.SaveToFile(SaveToFile, "Xml File|*.xml");
        }

        public void LoadFromFile(object sender, RoutedEventArgs e)
        {
            CamCore.FileOperations.LoadFromFile(LoadFromFile, "Xml File|*.xml");
        }

        public void LoadFromFile(Stream file)
        {
            XmlDocument dataDoc = new XmlDocument();
            dataDoc.Load(file);

            _gridsList.Clear();
            XmlNodeList grids = dataDoc.GetElementsByTagName("Grid");
            foreach (XmlNode gridNode in grids)
            {
                RealGridData grid = new RealGridData();
                var gridNum = gridNode.Attributes["num"];
                if (gridNum != null)
                    grid.Num = int.Parse(gridNum.Value);
                var gridLabel = gridNode.Attributes["label"];
                if (gridLabel != null)
                    grid.Label = gridLabel.Value;

                XmlNode widthNode = gridNode.SelectSingleNode("child::Width");
                    var wx = widthNode.Attributes["X"];
                    grid.WidthX = double.Parse(wx.Value);
                var wy = widthNode.Attributes["Y"];
                grid.WidthY = double.Parse(wy.Value);
                var wz = widthNode.Attributes["Z"];
                grid.WidthZ = double.Parse(wz.Value);

                XmlNode heightNode = gridNode.SelectSingleNode("child::Height");
                var hx = heightNode.Attributes["X"];
                grid.HeightX = double.Parse(hx.Value);
                var hy = heightNode.Attributes["Y"];
                grid.HeightY = double.Parse(hy.Value);
                var hz = heightNode.Attributes["Z"];
                grid.HeightZ = double.Parse(hz.Value);

                XmlNode zeroNode = gridNode.SelectSingleNode("child::Zero");
                var zx = zeroNode.Attributes["X"];
                grid.ZeroX = double.Parse(zx.Value);
                var zy = zeroNode.Attributes["Y"];
                grid.ZeroY = double.Parse(zy.Value);
                var zz = zeroNode.Attributes["Z"];
                grid.ZeroZ = double.Parse(zz.Value);

                _gridsList.Add(grid);
            }
        }

        public void SaveToFile(Stream file)
        {
            XmlDocument dataDoc = new XmlDocument();
            var rootNode = dataDoc.CreateElement("Grids");

            foreach (var grid in _gridsList)
            {
                var gridNode = dataDoc.CreateElement("Grid");
                var gridAttNum = dataDoc.CreateAttribute("num");
                gridAttNum.Value = grid.Num.ToString();
                var gridAttLabel = dataDoc.CreateAttribute("label");
                gridAttLabel.Value = grid.Label;
                gridNode.Attributes.Append(gridAttNum);
                gridNode.Attributes.Append(gridAttLabel);

                var gridWidth = dataDoc.CreateElement("Width");
                var widthAttX = dataDoc.CreateAttribute("X");
                widthAttX.Value = grid.WidthX.ToString();
                var widthAttY = dataDoc.CreateAttribute("Y");
                widthAttY.Value = grid.WidthY.ToString();
                var widthAttZ = dataDoc.CreateAttribute("Z");
                widthAttZ.Value = grid.WidthZ.ToString();
                gridWidth.Attributes.Append(widthAttX);
                gridWidth.Attributes.Append(widthAttY);
                gridWidth.Attributes.Append(widthAttZ);
                gridNode.AppendChild(gridWidth);

                var gridHeight = dataDoc.CreateElement("Height");
                var heightAttX = dataDoc.CreateAttribute("X");
                heightAttX.Value = grid.HeightX.ToString();
                var heightAttY = dataDoc.CreateAttribute("Y");
                heightAttY.Value = grid.HeightY.ToString();
                var heightAttZ = dataDoc.CreateAttribute("Z");
                heightAttZ.Value = grid.HeightZ.ToString();
                gridHeight.Attributes.Append(heightAttX);
                gridHeight.Attributes.Append(heightAttY);
                gridHeight.Attributes.Append(heightAttZ);
                gridNode.AppendChild(gridHeight);

                var gridZero = dataDoc.CreateElement("Zero");
                var zeroAttX = dataDoc.CreateAttribute("X");
                zeroAttX.Value = grid.ZeroX.ToString();
                var zeroAttY = dataDoc.CreateAttribute("Y");
                zeroAttY.Value = grid.ZeroY.ToString();
                var zeroAttZ = dataDoc.CreateAttribute("Z");
                zeroAttZ.Value = grid.ZeroZ.ToString();
                gridZero.Attributes.Append(zeroAttX);
                gridZero.Attributes.Append(zeroAttY);
                gridZero.Attributes.Append(zeroAttZ);
                gridNode.AppendChild(gridZero);

                rootNode.AppendChild(gridNode);
            }

            dataDoc.InsertAfter(rootNode, dataDoc.DocumentElement);
            dataDoc.Save(file);
        }
    }
}
