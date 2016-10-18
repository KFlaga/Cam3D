using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CamImageProcessing;
using CamImageProcessing.ImageMatching;
using System.ComponentModel;
using CamCore;
using System.IO;
using System.Xml;
using MathNet.Numerics.LinearAlgebra;

namespace ImageMatchingModule
{
    public partial class DisparityRefinementTab : UserControl
    {
        DisparityRange _range = new DisparityRange();
        ColorImage _dispImage = new ColorImage();
        DisparityMap _baseLeft;
        DisparityMap _baseRight;

        public DisparityMap MapLeftBase
        {
            get { return _baseLeft; }
            set { _baseLeft = value; }
        }

        public DisparityMap MapRightBase
        {
            get { return _baseRight; }
            set { _baseRight = value; }
        }

        DisparityMap _finalMap;
        public DisparityMap MapLeftCurrent 
        {
            get { return _finalMap; }
            set
            {
                _finalMap = value;
                _disparityMapImage.Map = _finalMap;
            }
        }
        public DisparityMap MapRightCurrent { get; set; }

        DisparityMap _storedLeft;
        DisparityMap _storedRight;
        
        public Matrix<double> ImageLeft { get; set; }
        public Matrix<double> ImageRight { get; set; }

        public event EventHandler<EventArgs> RequsestDisparityMapsUpdate; 

        private BindingList<RefinementBlock> _refinerBlocks = new BindingList<RefinementBlock>();

        public DisparityRefinementTab()
        {
            InitializeComponent();

            AddNewBlock();
            _refinerBlocksView.ItemsSource = _refinerBlocks;
        }

        private void ResetMaps(object sender, RoutedEventArgs e)
        {
            MapLeftCurrent = (DisparityMap)MapLeftBase.Clone();
            MapRightCurrent = (DisparityMap)MapRightBase.Clone();
        }

        private void UpdateMaps(object sender, RoutedEventArgs e)
        {
            RequsestDisparityMapsUpdate(this, new EventArgs());
        }

        private void ApplyOnBase(object sender, RoutedEventArgs e)
        {
            if(_baseLeft == null || _baseRight == null)
                return;

            int index = (int)((Button)sender).Tag;
            RefinementBlock block = _refinerBlocks[index];

            var refiner = block.Refiner;
            refiner.ImageLeft = ImageLeft;
            refiner.ImageRight = ImageRight;
            refiner.MapLeft = (DisparityMap)_baseLeft.Clone();
            refiner.MapRight = (DisparityMap)_baseRight.Clone();
            refiner.Init();
            refiner.RefineMaps();
            MapLeftCurrent = refiner.MapLeft;
            MapRightCurrent = refiner.MapRight;
        }

        private void ApplyOnCurrent(object sender, RoutedEventArgs e)
        {
            if(MapLeftCurrent == null || MapRightCurrent == null)
                return;

            int index = (int)((Button)sender).Tag;
            RefinementBlock block = _refinerBlocks[index];

            var refiner = block.Refiner;
            refiner.ImageLeft = ImageLeft;
            refiner.ImageRight = ImageRight;
            refiner.MapLeft = MapLeftCurrent;
            refiner.MapRight = MapRightCurrent;
            refiner.Init();
            refiner.RefineMaps();
            MapLeftCurrent = refiner.MapLeft;
            MapRightCurrent = refiner.MapRight;
        }

        private void ApplyAboveOnBase(object sender, RoutedEventArgs e)
        {
            if(_baseLeft == null || _baseRight == null)
                return;

            int index = (int)((Button)sender).Tag;
            RefinementBlock block = _refinerBlocks[index];
            MapLeftCurrent = (DisparityMap)_baseLeft.Clone();
            MapRightCurrent = (DisparityMap)_baseRight.Clone();

            for(int i = 0; i <= index; ++i)
            {
                var refiner = _refinerBlocks[i].Refiner;

                refiner.ImageLeft = ImageLeft;
                refiner.ImageRight = ImageRight;
                refiner.MapLeft = MapLeftCurrent;
                refiner.MapRight = MapRightCurrent;
                refiner.Init();
                refiner.RefineMaps();
                MapLeftCurrent = refiner.MapLeft;
                MapRightCurrent = refiner.MapRight;
            }
        }

        private void ApplyAboveOnCurrent(object sender, RoutedEventArgs e)
        {
            if(MapLeftCurrent == null || MapRightCurrent == null)
                return;

            int index = (int)((Button)sender).Tag;
            RefinementBlock block = _refinerBlocks[index];

            for(int i = 0; i <= index; ++i)
            {
                var refiner = _refinerBlocks[i].Refiner;

                refiner.ImageLeft = ImageLeft;
                refiner.ImageRight = ImageRight;
                refiner.MapLeft = MapLeftCurrent;
                refiner.MapRight = MapRightCurrent;
                refiner.Init();
                refiner.RefineMaps();
                MapLeftCurrent = refiner.MapLeft;
                MapRightCurrent = refiner.MapRight;
            }
        }

        private void ChangeRefiner(object sender, RoutedEventArgs e)
        {
            Button triggeringButton = sender as Button;
            int index = (int)triggeringButton.Tag;
            RefinementBlock block = _refinerBlocks[index];
            
            RefinerChooseWindow refinerWindow = new RefinerChooseWindow();
            bool newBlockChoosen = (block.Index == _refinerBlocks.Count - 1);
            if(block.IsRefinerSet)
            {
                refinerWindow.ChoosenRefiner = block.Refiner;
            }

            refinerWindow.Closing += (s, ea) =>
            {
                if(refinerWindow.Accepted)
                {
                    if(refinerWindow.ChoosenRefiner != null)
                    {
                        block.Refiner = refinerWindow.ChoosenRefiner;
                        if(newBlockChoosen)
                            AddNewBlock();
                    }
                }
                else if(refinerWindow.RemoveCurrent)
                {
                    if(newBlockChoosen)
                        return;

                    RemoveBlock(block.Index);
                }
            };

            refinerWindow.ShowDialog();
        }

        void AddNewBlock()
        {
            RefinementBlock emptyBlock = new RefinementBlock()
            {
                Index = _refinerBlocks.Count,
                Refiner = null
            };

            _refinerBlocks.Add(emptyBlock);
        }

        void RemoveBlock(int index)
        {
            for(int i = index + 1; i < _refinerBlocks.Count; ++i)
            {
                _refinerBlocks[i].Index = i - 1;
            }
            _refinerBlocks.RemoveAt(index);
        }

        private void StoreMap(object sender, RoutedEventArgs e)
        {
            _storedLeft = (DisparityMap)MapLeftCurrent.Clone();
            _storedRight = (DisparityMap)MapRightCurrent.Clone();
        }

        private void RestoreMap(object sender, RoutedEventArgs e)
        {
            MapLeftCurrent = (DisparityMap)_storedLeft.Clone();
            MapRightCurrent = (DisparityMap)_storedRight.Clone();
        }

        private void SaveMapToFile(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(SaveMapToFile, "Xml File|*.xml");
        }

        private void LoadMapFromFile(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile(LoadMapFromFile, "Xml File|*.xml");
        }

        private void SaveMapToFile(Stream file, string path)
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlNode mapNode = MapLeftCurrent.CreateMapNode(xmlDoc);
            xmlDoc.InsertAfter(mapNode, xmlDoc.DocumentElement);

            xmlDoc.Save(file);
        }

        private void LoadMapFromFile(Stream file, string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            XmlNode mapNode = xmlDoc.GetElementsByTagName("DisparityMap")[0];
            MapLeftCurrent = DisparityMap.CreateFromNode(mapNode);
        }
    }

    public class RefinementBlock : INotifyPropertyChanged
    {
        DisparityRefinement _refiner;
        public DisparityRefinement Refiner
        {
            get { return _refiner; }
            set
            {
                _refiner = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Refiner"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRefinerSet"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }

        int _index;
        public int Index
        {
            get { return _index; }
            set
            {
                _index = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Index"));
            }
        }

        public bool IsRefinerSet { get { return Refiner != null; } }
        public string Name {  get { return ToString(); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Refiner != null ? Refiner.ToString() :
                "Click To Choose Refinement Step";
        }
    }
}
