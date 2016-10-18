using CamCore;
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
            for(int grid = 0; grid < _gridsList.Count; grid++)
            {
                _savedList[grid].Update();
            }
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
            BindingOperations.ClearBinding(_tbCols, TextBox.TextProperty);
            BindingOperations.ClearBinding(_tbRows, TextBox.TextProperty);

            _tbBLX.ClearValueChangedEvent();
            _tbBLY.ClearValueChangedEvent();
            _tbBLZ.ClearValueChangedEvent();
            _tbBRX.ClearValueChangedEvent();
            _tbBRY.ClearValueChangedEvent();
            _tbBRZ.ClearValueChangedEvent();
            _tbTLX.ClearValueChangedEvent();
            _tbTLY.ClearValueChangedEvent();
            _tbTLZ.ClearValueChangedEvent();
            _tbTRX.ClearValueChangedEvent();
            _tbTRY.ClearValueChangedEvent();
            _tbTRZ.ClearValueChangedEvent();

            _tbGridNum.Text = "";
            _tbLabel.Text = "";
            _tbRows.CurrentValue = 0;
            _tbCols.CurrentValue = 0;
            _tbBLX.CurrentValue = 0;
            _tbBLY.CurrentValue = 0;
            _tbBLZ.CurrentValue = 0;
            _tbBRX.CurrentValue = 0;
            _tbBRY.CurrentValue = 0;
            _tbBRZ.CurrentValue = 0;
            _tbTLX.CurrentValue = 0;
            _tbTLY.CurrentValue = 0;
            _tbTLZ.CurrentValue = 0;
            _tbTRX.CurrentValue = 0;
            _tbTRY.CurrentValue = 0;
            _tbTRZ.CurrentValue = 0;
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
                _tbCols.SetBinding(TextBox.TextProperty, new Binding("Columns")
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

                _tbBLX.CurrentValue = addgrid.BotLeft.X;
                _tbBLX.ValueChanged += (s, ea) => { addgrid.BotLeft.X = ea.NewValue; };
                _tbBLY.CurrentValue = addgrid.BotLeft.Y;
                _tbBLY.ValueChanged += (s, ea) => { addgrid.BotLeft.Y = ea.NewValue; };
                _tbBLZ.CurrentValue = addgrid.BotLeft.Z;
                _tbBLZ.ValueChanged += (s, ea) => { addgrid.BotLeft.Z = ea.NewValue; };
                _tbBRX.CurrentValue = addgrid.BotRight.X;
                _tbBRX.ValueChanged += (s, ea) => { addgrid.BotRight.X = ea.NewValue; };
                _tbBRY.CurrentValue = addgrid.BotRight.Y;
                _tbBRY.ValueChanged += (s, ea) => { addgrid.BotRight.Y = ea.NewValue; };
                _tbBRZ.CurrentValue = addgrid.BotRight.Z;
                _tbBRZ.ValueChanged += (s, ea) => { addgrid.BotRight.Z = ea.NewValue; };
                _tbTLX.CurrentValue = addgrid.TopLeft.X;
                _tbTLX.ValueChanged += (s, ea) => { addgrid.TopLeft.X = ea.NewValue; };
                _tbTLY.CurrentValue = addgrid.TopLeft.Y;
                _tbTLY.ValueChanged += (s, ea) => { addgrid.TopLeft.Y = ea.NewValue; };
                _tbTLZ.CurrentValue = addgrid.TopLeft.Z;
                _tbTLZ.ValueChanged += (s, ea) => { addgrid.TopLeft.Z = ea.NewValue; };
                _tbTRX.CurrentValue = addgrid.TopRight.X;
                _tbTRX.ValueChanged += (s, ea) => { addgrid.TopRight.X = ea.NewValue; };
                _tbTRY.CurrentValue = addgrid.TopRight.Y;
                _tbTRY.ValueChanged += (s, ea) => { addgrid.TopRight.Y = ea.NewValue; };
                _tbTRZ.CurrentValue = addgrid.TopRight.Z;
                _tbTRZ.ValueChanged += (s, ea) => { addgrid.TopRight.Z = ea.NewValue; };
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

        public void LoadFromFile(Stream file, string path)
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

                XmlNode topleftNode = gridNode.SelectSingleNode("child::TopLeft");
                grid.TopLeft = Vector3.CreateFromXmlNode(topleftNode);

                XmlNode toprightNode = gridNode.SelectSingleNode("child::TopRight");
                grid.TopRight = Vector3.CreateFromXmlNode(toprightNode);

                XmlNode botleftNode = gridNode.SelectSingleNode("child::BotLeft");
                grid.BotLeft = Vector3.CreateFromXmlNode(botleftNode);

                XmlNode botrightNode = gridNode.SelectSingleNode("child::BotRight");
                grid.BotRight = Vector3.CreateFromXmlNode(botrightNode);

                var rowsNode = gridNode.SelectSingleNode("child::Rows");
                if(rowsNode != null)
                    grid.Rows = int.Parse(rowsNode.Attributes["count"].Value);

                var colsNode = gridNode.SelectSingleNode("child::Columns");
                if(colsNode != null)
                    grid.Columns = int.Parse(colsNode.Attributes["count"].Value);

                grid.Update();

                _gridsList.Add(grid);
            }
        }

        public void SaveToFile(Stream file, string path)
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
                
                gridNode.AppendChild(grid.TopLeft.CreateXmlNode(dataDoc, "TopLeft"));
                gridNode.AppendChild(grid.TopRight.CreateXmlNode(dataDoc, "TopRight"));
                gridNode.AppendChild(grid.BotLeft.CreateXmlNode(dataDoc, "BotLeft"));
                gridNode.AppendChild(grid.BotRight.CreateXmlNode(dataDoc, "BotRight"));

                XmlNode rowsNode = dataDoc.CreateElement("Rows");
                var rowsAtt = dataDoc.CreateAttribute("count");
                rowsAtt.Value = grid.Rows.ToString();
                rowsNode.Attributes.Append(rowsAtt);
                gridNode.AppendChild(rowsNode);

                XmlNode colsNode = dataDoc.CreateElement("Columns");
                var colsAtt = dataDoc.CreateAttribute("count");
                colsAtt.Value = grid.Columns.ToString();
                colsNode.Attributes.Append(colsAtt);
                gridNode.AppendChild(colsNode);

                rootNode.AppendChild(gridNode);
            }

            dataDoc.InsertAfter(rootNode, dataDoc.DocumentElement);
            dataDoc.Save(file);
        }

        public void UpdateFromP1P4(object sender, RoutedEventArgs e)
        {
            RealGridData grid = (RealGridData)_gridListView.SelectedItem;

            Vector3 p1 = new Vector3(_tbP1X.CurrentValue, _tbP1Y.CurrentValue, _tbP1Z.CurrentValue);
            Vector3 p4 = new Vector3(_tbP4X.CurrentValue, _tbP4Y.CurrentValue, _tbP4Z.CurrentValue);
            Vector3 p1p = new Vector3(_tbP1pX.CurrentValue, _tbP1pY.CurrentValue, _tbP1pZ.CurrentValue);
            Vector3 p4p = new Vector3(_tbP4pX.CurrentValue, _tbP4pY.CurrentValue, _tbP4pZ.CurrentValue);

            grid.FillFromP1P4(p1, p4, p1p, p4p);
        }
    }
}
