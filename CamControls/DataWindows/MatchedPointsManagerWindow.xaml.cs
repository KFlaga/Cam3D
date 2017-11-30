using CamCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace CamControls
{
    /// <summary>
    /// Interaction logic for MatchedPointsManagerWindow.xaml
    /// </summary>
    public partial class MatchedPointsManagerWindow : Window
    {
        private BindingList<Vector2Pair> _pointList;
        private List<Vector2Pair> _savedList;
        public List<Vector2Pair> Points
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

        public MatchedPointsManagerWindow()
        {
            InitializeComponent();
            _savedList = new List<Vector2Pair>();
            _pointList = new BindingList<Vector2Pair>();
            _pointListView.ItemsSource = _pointList;
        }

        private void Accept(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveToFile(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(SaveToFile, "Xml File|*.xml");
        }

        private void LoadFromFile(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile(LoadFromFile, "Xml File|*.xml");
        }

        public void LoadFromFile(Stream file, string path)
        {
            _savedList = XmlSerialisation.CreateFromFile<List<Vector2Pair>>(file);
            _pointList.Clear();
            foreach(var p in _savedList)
            {
                _pointList.Add(p);
            }
        }

        public void SaveToFile(Stream file, string path)
        {
            XmlSerialisation.SaveToFile(_pointList.ToList(), file);
        }
    }
}
