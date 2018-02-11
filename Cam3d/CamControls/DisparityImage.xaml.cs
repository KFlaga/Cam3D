using CamCore;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;

namespace CamControls
{
    public partial class DisparityImage : UserControl
    {
        DisparityRange _range = new DisparityRange();
        DisparityMap _map;
        ColorImage _image = new ColorImage();
        DisparityBox _dbox = new DisparityBox();

        public DisparityMap Map
        {
            get { return _map; }
            set
            {
                ResetDispBox();

                _map = value;
                // Find min/max
                if(false == RangeFrozen)
                {
                    Range.Min = Range.Max = _map[0, 0].DX;
                    for(int r = 0; r < _map.RowCount; ++r)
                    {
                        for(int c = 0; c < _map.ColumnCount; ++c)
                        {
                            if(_map[r, c].IsValid())
                            {
                                Range.Min = Math.Min(Range.Min, _map[r, c].DX);
                                Range.Max = Math.Max(Range.Max, _map[r, c].DX);
                            }
                        }
                    }
                    Range.TempMax = Range.Max;
                    Range.TempMin = Range.Min;

                    _legend.Range = _range;
                }
                UpdateImage();
            }
        }

        public ColorImage Image
        {
            get { return _image; }
        }

        public DisparityRange Range
        {
            get { return _range; }
        }

        public DisparityLegend Legend
        {
            get { return _legend; }
        }

        //  private Image _dispImage { get { return (Image)_imageControl.Child; } }
        private Image _dispImage { get { return (Image)_imageControl; } }

        public event EventHandler<EventArgs> MapLoaded;

        public bool RangeFrozen { get; set; } = false;

        bool _enableSaveLoad = true;
        public bool IsSaveLoadEnabled
        {
            get { return _enableSaveLoad; }
            set
            {
                _enableSaveLoad = value;
                _butSave.IsEnabled = _enableSaveLoad;
                _butLoad.IsEnabled = _enableSaveLoad;
            }
        }

        private void SaveMap(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(SaveMap, "Xml File|*.xml");
        }

        private void LoadMap(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile(LoadMap, "Xml File|*.xml");
        }

        private void SaveMap(Stream file, string path)
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlNode mapNode = Map.CreateMapNode(xmlDoc);
            xmlDoc.InsertAfter(mapNode, xmlDoc.DocumentElement);

            xmlDoc.Save(file);
        }

        private void LoadMap(Stream file, string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            XmlNode mapNode = xmlDoc.GetElementsByTagName("DisparityMap")[0];
            Map = DisparityMap.CreateFromNode(mapNode);

            MapLoaded?.Invoke(this, new EventArgs());
        }

        public DisparityImage()
        {
            InitializeComponent();

            Canvas.SetLeft(_dbox, 10000);
            Canvas.SetTop(_dbox, 10000);
            _dispBoxCanvas.Children.Add(_dbox);

            _dispImage.MouseDown += MousePressed;
        }

        public void UpdateImage()
        {
            if(_map != null)
            {
                int rows = _map.Disparities.GetLength(0);
                int cols = _map.Disparities.GetLength(1);
                _image[RGBChannel.Red] = new DenseMatrix(rows, cols);
                _image[RGBChannel.Green] = new DenseMatrix(rows, cols);
                _image[RGBChannel.Blue] = new DenseMatrix(rows, cols);
                
                for(int r = 0; r < rows; ++r)
                {
                    for(int c = 0; c < cols; ++c)
                    {
                        int idx1 = _range.GetDisparityIndex((int)_map[r, c].SubDX);
                        int idx2 = _range.GetDisparityIndex((int)_map[r, c].SubDX + 1);
                        double ratio = Math.Abs(_map[r, c].SubDX - (double)((int)_map[r, c].SubDX));
                        if((_map[r, c].Flags & (int)DisparityFlags.Valid) != 0 &&
                            !(idx1 < 0 || idx2 >= _range.GetTempDisparityRange()))
                        {
                            _image[RGBChannel.Red][r, c] = _range.Colors[idx2][0] * (1 - ratio) + _range.Colors[idx1][0] * ratio;
                            _image[RGBChannel.Green][r, c] = _range.Colors[idx2][1] * (1 - ratio) + _range.Colors[idx1][1] * ratio;
                            _image[RGBChannel.Blue][r, c] = _range.Colors[idx2][2] * (1 - ratio) + _range.Colors[idx1][2] * ratio;
                        }
                        else if(idx1 < 0 || idx2 >= _range.GetTempDisparityRange())
                        {
                            _image[RGBChannel.Red][r, c] = 0.5;
                            _image[RGBChannel.Green][r, c] = 0.5;
                            _image[RGBChannel.Blue][r, c] = 0.5;
                        }
                        else //if((_map[r, c].Flags & (int)DisparityFlags.Invalid) != 0)
                        {
                            _image[RGBChannel.Red][r, c] = 0.0;
                            _image[RGBChannel.Green][r, c] = 0.0;
                            _image[RGBChannel.Blue][r, c] = 0.0;
                        }
                    }
                }
                
                _dispImage.Source = _image.ToBitmapSource();
            }

        }

        private void MousePressed(object sender, MouseEventArgs e)
        {
            // Show DisparityBox with disparity from choosen pixel
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                Point mpos = Mouse.GetPosition(_dispImage);
                MoveDispBox(new Vector2(mpos.X, mpos.Y));
            }
            else if(e.RightButton == MouseButtonState.Pressed)
            {
                ResetDispBox();
            }
        }

        private void MoveDispBox(Vector2 mpos)
        {
            Vector2 pixel = new Vector2(mpos.X, mpos.Y);
            pixel.X *= _image.ColumnCount / _dispImage.ActualWidth;
            pixel.Y *= _image.RowCount / _dispImage.ActualHeight;


            // Get/set disparity
            Disparity disp = _map[(int)pixel.Y, (int)pixel.X];

            _dbox.Disparity = disp;

            Point mpos2 = Mouse.GetPosition(_dispBoxCanvas);

            // Check if box fits
            double widthRem = _dispImage.ActualWidth - mpos.X;
            double heightRem = _dispImage.ActualHeight - mpos.Y;
            Vector2 topLeftPos = new Vector2(mpos2.X, mpos2.Y);
            Vector2 posShift;
            if(widthRem < _dbox.Width && heightRem < _dbox.Height)
            {
                // Set bot-right of box to be mpos + (-2,-2)
                posShift = new Vector2(-_dbox.Width - 2.0, -_dbox.Height - 2.0);
            }
            else if(widthRem < _dbox.Width)
            {
                // Set top-right of box to be mpos + (-2,2)
                posShift = new Vector2(-_dbox.Width - 2.0, 2.0);
            }
            else if(heightRem < _dbox.Height)
            {
                // Set bot-left of box to be mpos + (2,-2)
                posShift = new Vector2(2.0, -_dbox.Height - 2.0);
            }
            else
            {
                // Set top-left of box to be mpos + (2,2)
                posShift = new Vector2(20.0, 20.0);
            }

            topLeftPos = topLeftPos + posShift;
            // _dbox.Margin = new Thickness(topLeftPos.X, topLeftPos.Y, 0, 0);
            Canvas.SetLeft(_dbox, topLeftPos.X);
            Canvas.SetTop(_dbox, topLeftPos.Y);
        }

        private void ResetDispBox()
        {
            // _dbox.Margin = new Thickness(10000, 10000, 0, 0);
            Canvas.SetLeft(_dbox, 10000);
            Canvas.SetTop(_dbox, 10000);
        }

        private void SelectRange(object sender, RoutedEventArgs e)
        {
            RangeSelectionPanel rangeSelect = new RangeSelectionPanel();
            rangeSelect.Range = _range;
            rangeSelect.ShowDialog();
            if(rangeSelect.Accepted)
            {
                _legend.Range = _range;
                UpdateImage();
            }
        }

        private void FreezeRange(object sender, RoutedEventArgs e)
        {
            RangeFrozen = true;
        }

        private void UnfreezeRange(object sender, RoutedEventArgs e)
        {
            RangeFrozen = false;
        }
    }
}
