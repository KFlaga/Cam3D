using CamCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace CalibrationModule
{
    /// <summary>
    /// Interaction logic for CalibrationPointsManagerWindow.xaml
    /// </summary>
    public partial class CalibrationLinesManagerWindow : Window
    {
        private BindingList<int> _linesNumbers;
        private BindingList<Vector2> _currentLine;

        private List<List<Vector2>> _linesList;
        public List<List<Vector2>> CalibrationLines
        {
            get
            {
                return _linesList;
            }
            set
            {
                _linesNumbers.Clear();
                _linesList.Clear();
                int i = 0;
                foreach(var line in value)
                {
                    _linesNumbers.Add(i);
                    _linesList.Add(line);
                    ++i;
                }
            }
        }

        public CalibrationLinesManagerWindow()
        {
            InitializeComponent();
            _linesList = new List<List<Vector2>>();
            _linesNumbers = new BindingList<int>();
            _linesstView.ItemsSource = _linesNumbers;

            _currentLine = new BindingList<Vector2>();
            _pointsView.ItemsSource = _currentLine;
        }

        private void AddLine(object sender, RoutedEventArgs e)
        {
            List<Vector2> line = new List<Vector2>();
            _linesList.Add(line);
            _linesNumbers.Add(_linesList.Count - 1);
        }

        private void DeleteLine(object sender, RoutedEventArgs e)
        {
            if(_linesstView.SelectedIndex != -1)
            {
                if(_linesstView.Items.Count == 1)
                { 
                    _linesList.Clear();
                }
                else
                {
                    int toRemove = (int)_linesstView.SelectedItem;
                    _currentLine.Clear();
                    _linesList.RemoveAt(toRemove);
                }

                _linesNumbers.Clear();
                for(int idx = 0; idx < _linesList.Count; idx++)
                {
                    _linesNumbers.Add(idx);
                }
            }
        }

        private void DeleteAllLines(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(this, "Confirm clearing of all points",
                "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
            if(result == MessageBoxResult.OK)
            {
                if(_linesNumbers.Count > 0)
                {
                    _linesList.Clear();
                    _linesNumbers.Clear();
                }
            }
        }

        private void Accept(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void SelectLine(object sender, SelectionChangedEventArgs e)
        {
            if(_linesstView.SelectedIndex == -1)
                return;

            _currentLine.Clear();
            int idx = (int)_linesstView.SelectedItem;
            var line = _linesList[idx];
            for(int i = 0; i < line.Count; ++i)
            {
                _currentLine.Add(line[i]);
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

        public void LoadFromFile(Stream file, string path)
        {
            _currentLine.Clear();
            _linesList.Clear();
            _linesNumbers.Clear();

            _linesList.AddRange(XmlSerialisation.CreateFromFile<List<List<Vector2>>>(file));
            for(int i = 0; i < _linesList.Count; ++i) { _linesNumbers.Add(i); }
        }

        public void SaveToFile(Stream file, string path)
        {
            XmlSerialisation.SaveToFile(_linesList, file);
        }
    }
}
