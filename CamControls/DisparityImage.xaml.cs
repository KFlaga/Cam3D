using CamCore;
using CamImageProcessing;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;

namespace CamControls
{
    public partial class DisparityImage : UserControl
    {
        DisparityRange _rangeX = new DisparityRange();
        DisparityRange _rangeY = new DisparityRange();
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
                RangeX.Min = RangeX.Max = _map[0, 0].DX;
                RangeY.Min = RangeY.Max = _map[0, 0].DY;
                for(int r = 0; r < _map.RowCount; ++r)
                {
                    for(int c = 0; c < _map.ColumnCount; ++c)
                    {
                        if(_map[r, c].IsValid())
                        {
                            RangeX.Min = Math.Min(RangeX.Min, _map[r, c].DX);
                            RangeX.Max = Math.Max(RangeX.Max, _map[r, c].DX);
                            RangeY.Min = Math.Min(RangeY.Min, _map[r, c].DY);
                            RangeY.Max = Math.Max(RangeY.Max, _map[r, c].DY);
                        }
                    }
                }
                RangeX.TempMax = RangeX.Max;
                RangeX.TempMin = RangeX.Min;
                RangeY.TempMax = RangeY.Min;
                RangeY.TempMin = RangeY.Max;

                if(_showDX)
                    _legend.Range = _rangeX;
                else
                    _legend.Range = _rangeY;
                UpdateImage();
            }
        }

        bool _showDX = true;
        public bool IsShownDX
        {
            get { return _showDX; }
            set
            {
                if(value != _showDX)
                {
                    _showDX = value;

                    if(_showDX)
                        _legend.Range = _rangeX;
                    else
                        _legend.Range = _rangeY;
                    UpdateImage();
                }
            }
        }

        public ColorImage Image
        {
            get { return _image; }
        }

        public DisparityRange RangeX
        {
            get { return _rangeX; }
        }

        public DisparityRange RangeY
        {
            get { return _rangeY; }
        }

        public DisparityLegend Legend
        {
            get { return _legend; }
        }

        //  private Image _dispImage { get { return (Image)_imageControl.Child; } }
        private Image _dispImage { get { return (Image)_imageControl; } }

        public event EventHandler<EventArgs> MapLoaded;

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
        
        private void _dispDirectionShowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsShownDX = false;
        }

        private void _dispDirectionShowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsShownDX = true;
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

                if(_showDX)
                {
                    for(int r = 0; r < rows; ++r)
                    {
                        for(int c = 0; c < cols; ++c)
                        {
                            int idx = _rangeX.GetDisparityIndex(_map[r, c].DX);
                            if((_map[r, c].Flags & (int)DisparityFlags.Valid) != 0 &&
                                !(idx < 0 || idx >= _rangeX.GetTempDisparityRange()))
                            {
                                _image[RGBChannel.Red][r, c] = _rangeX.Colors[idx][0];
                                _image[RGBChannel.Green][r, c] = _rangeX.Colors[idx][1];
                                _image[RGBChannel.Blue][r, c] = _rangeX.Colors[idx][2];
                            }
                            else if(idx < 0 || idx >= _rangeX.GetTempDisparityRange())
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
                }
                else
                {
                    for(int r = 0; r < rows; ++r)
                    {
                        for(int c = 0; c < cols; ++c)
                        {
                            int idx = _rangeY.GetDisparityIndex(_map[r, c].DY);
                            if((_map[r, c].Flags & (int)DisparityFlags.Valid) != 0 &&
                                !(idx < 0 || idx >= _rangeY.GetTempDisparityRange()))
                            { 
                                _image[RGBChannel.Red][r, c] = _rangeY.Colors[idx][0];
                                _image[RGBChannel.Green][r, c] = _rangeY.Colors[idx][1];
                                _image[RGBChannel.Blue][r, c] = _rangeY.Colors[idx][2];
                            }
                            else if(idx < 0 || idx >= _rangeY.GetTempDisparityRange())
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
            rangeSelect.RangeX = _rangeX;
            rangeSelect.RangeY = _rangeY;
            rangeSelect.ShowDialog();
            if(rangeSelect.Accepted)
            {
                if(_showDX)
                    _legend.Range = _rangeX;
                else
                    _legend.Range = _rangeY;
                UpdateImage();
            }
        }
    }
}
