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
    public partial class ImagePointsManagerWindow : Window
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
                foreach(var point in value)
                {
                    _pointList.Add(point);
                    _savedList.Add(point);
                }
            }
        }

        public ImagePointsManagerWindow()
        {
            InitializeComponent();
            _savedList = new List<Vector2>();
            _pointList = new BindingList<Vector2>();
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
            _savedList = XmlSerialisation.CreateFromFile<List<Vector2>>(file);
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
