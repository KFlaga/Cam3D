using CamAlgorithms.Calibration;
using CamCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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

        private void SaveToFile(object sender, RoutedEventArgs e)
        {
            CamCore.FileOperations.SaveToFile(SaveToFile, "Xml File|*.xml");
        }

        private void LoadFromFile(object sender, RoutedEventArgs e)
        {
            CamCore.FileOperations.LoadFromFile(LoadFromFile, "Xml File|*.xml");
        }

        private void LoadFromFile(Stream file, string path)
        {
            _savedList = CamCore.XmlSerialisation.CreateFromFile<List<RealGridData>>(file);

            _gridsList.Clear();
            foreach(var grid in _savedList)
            {
                _gridsList.Add(grid);
            }
        }

        private void SaveToFile(Stream file, string path)
        {
            CamCore.XmlSerialisation.SaveToFile(_savedList, file);
        }

        private void UpdateFromP1P4(object sender, RoutedEventArgs e)
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
