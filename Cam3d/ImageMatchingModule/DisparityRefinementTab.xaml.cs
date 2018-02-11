using System;
using System.Windows;
using System.Windows.Controls;
using CamAlgorithms;
using CamAlgorithms.ImageMatching;
using System.ComponentModel;
using CamCore;
using CamControls;

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

        ColorImage _imgLeft;
        ColorImage _imgRight;
        GrayScaleImage _imgGrayLeft;
        GrayScaleImage _imgGrayRight;
        public ColorImage ImageLeft
        {
            get { return _imgLeft; }
            set
            {
                _imgLeft = value;
                if(_imgLeft != null)
                {
                    _imgGrayLeft = new GrayScaleImage();
                    _imgGrayLeft.FromColorImage(_imgLeft);
                }
                else
                {
                    _imgGrayLeft = null;
                }
            }
        }
        public ColorImage ImageRight
        {
            get { return _imgRight; }
            set
            {
                _imgRight = value;
                if(_imgRight != null)
                {
                    _imgGrayRight = new GrayScaleImage();
                    _imgGrayRight.FromColorImage(_imgRight);
                }
                else
                {
                    _imgGrayRight = null;
                }
            }
        }

        public event EventHandler<EventArgs> RequsestDisparityMapsUpdate;
        public event EventHandler<EventArgs> RequsestImagesUpdate;

        private BindingList<RefinementBlock> _refinerBlocks = new BindingList<RefinementBlock>();

        public DisparityRefinementTab()
        {
            InitializeComponent();

            AddNewBlock();
            _refinerBlocksView.ItemsSource = _refinerBlocks;

            _disparityMapImage.MapLoaded += (s, e) =>
            {
                _finalMap = MapLeftBase = _disparityMapImage.Map;
                MapRightCurrent = MapRightBase = null;
            };
        }

        private void ResetMaps(object sender, RoutedEventArgs e)
        {
            if(MapLeftBase != null)
                MapLeftBase = (DisparityMap)MapLeftBase.Clone();
            else
                _finalMap = null;

            if(MapRightBase != null)
                MapRightCurrent = (DisparityMap)MapRightBase.Clone();
            else
                MapRightCurrent = null;
        }

        private void UpdateMaps(object sender, RoutedEventArgs e)
        {
            RequsestDisparityMapsUpdate?.Invoke(this, new EventArgs());
        }

        private void UpdateImages(object sender, RoutedEventArgs e)
        {
            RequsestImagesUpdate?.Invoke(this, new EventArgs());
        }

        private void ApplyOnBase(object sender, RoutedEventArgs e)
        {
            if(_baseLeft == null)
                return;

            int index = (int)((Button)sender).Tag;
            RefinementBlock block = _refinerBlocks[index];

            var refiner = block.Refiner;
            refiner.ImageLeft = _imgGrayLeft;
            refiner.ImageRight = _imgGrayRight;
            refiner.MapLeft = (DisparityMap)_baseLeft.Clone();
            refiner.MapRight = _baseRight != null ? (DisparityMap)_baseRight.Clone() : null;
            refiner.Init();
            refiner.RefineMaps();
            MapLeftCurrent = refiner.MapLeft;
            MapRightCurrent = refiner.MapRight;
        }

        private void ApplyOnCurrent(object sender, RoutedEventArgs e)
        {
            if(MapLeftCurrent == null)
                return;

            int index = (int)((Button)sender).Tag;
            RefinementBlock block = _refinerBlocks[index];

            var refiner = block.Refiner;
            refiner.ImageLeft = _imgGrayLeft;
            refiner.ImageRight = _imgGrayRight;
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

                refiner.ImageLeft = _imgGrayLeft;
                refiner.ImageRight = _imgGrayRight;
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

                refiner.ImageLeft = _imgGrayLeft;
                refiner.ImageRight = _imgGrayRight;
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
            _storedLeft = MapLeftCurrent != null ? (DisparityMap)MapLeftCurrent.Clone() : null;
            _storedRight = MapRightCurrent != null ? (DisparityMap)MapRightCurrent.Clone() : null;
        }

        private void RestoreMap(object sender, RoutedEventArgs e)
        {
            MapLeftCurrent = _storedLeft != null ? (DisparityMap)_storedLeft.Clone() : null;
            MapRightCurrent = _storedRight != null ? (DisparityMap)_storedRight.Clone() : null;
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
