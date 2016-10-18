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

namespace CamControls
{
    public partial class ColorPicker : Window
    {
        public static readonly DependencyProperty PickedColorProperty =
         DependencyProperty.Register("PickedColor", typeof(Color), typeof(ColorPicker), new
            PropertyMetadata(Colors.Black, new PropertyChangedCallback(OnPickedColorChanged)));

        public Color PickedColor
        {
            get { return (Color)GetValue(PickedColorProperty); }
            set { SetValue(PickedColorProperty, value); }
        }

        private static void OnPickedColorChanged(DependencyObject d,
           DependencyPropertyChangedEventArgs e)
        {
            ColorPicker cpicker = d as ColorPicker;
            cpicker.OnPickedColorChanged(e);
        }

        private void OnPickedColorChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateColorBoxes();
        }

        private WriteableBitmap _hueImage;
        private byte[] _hue;
        private double _hueAngle;
        const double _huePointer_ZeroMargin = 3.0;
        const double _huePointer_360Margin = (144.0 - _huePointer_ZeroMargin) / 360.0;

        private WriteableBitmap _bsImage;
        private double _saturation;
        private double _brightness;
        double[] _bsPointer_ZeroMargin = { 3.0, 11.0 };
        const double _bsPointer_360Margin = 140.0;

        const int R = 0;
        const int G = 1;
        const int B = 2;

        bool _colorChangedInternal = false;

        public ColorPicker()
        {
            InitializeComponent();

            _hue = new byte[3] { 255, 0, 0 };
            _brightness = 1.0;
            _saturation = 1.0;
            _hueAngle = 0.0;

            _tbBlue.LimitValue = true;
            _tbBlue.MaxValue = 255;
            _tbBlue.MinValue = 255;
            _tbGreen.LimitValue = true;
            _tbGreen.MaxValue = 255;
            _tbGreen.MinValue = 255;
            _tbRed.LimitValue = true;
            _tbRed.MaxValue = 255;
            _tbRed.MinValue = 255;

            FillHueField();
            FillBrightnessSaturationField();
            UpdateColorFromHSB();
            UpdateColorBoxes();
        }

        private void Accept(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void UpdateColorBoxes()
        {
            _tbBlue.SetNumber((uint)PickedColor.B);
            _tbGreen.SetNumber((uint)PickedColor.G);
            _tbRed.SetNumber((uint)PickedColor.R);
        }

        private void FillHueField()
        {
            int h = (int)(360.0 * _huePicker.Height / _huePicker.Width);
            _hueImage = new WriteableBitmap(360, h, 96, 96,
                PixelFormats.Rgb24, BitmapPalettes.Halftone256);
            // On X axis is hue angle : (x = 0 => a = 0 => red, x = 0.33... => a = 120 => green )
            int stride = _hueImage.BackBufferStride;
            byte[] bbuffer = new byte[3 * 361 * (h + 1)];
            byte d;
            for(int y = 0; y < h; ++y)
            {
                for(int x = 0; x < 60; ++x)
                {
                    d = (byte)x;
                    bbuffer[3 * x + stride * y] = 255;
                    bbuffer[3 * x + stride * y + 1] = (byte)(255 * d / 60);
                    bbuffer[3 * x + stride * y + 2] = 0;
                }

                for(int x = 60; x < 120; ++x)
                {
                    d = (byte)(x - 60);
                    bbuffer[3 * x + stride * y] = (byte)(255 - 255 * d / 60);
                    bbuffer[3 * x + stride * y + 1] = 255;
                    bbuffer[3 * x + stride * y + 2] = 0;
                }

                for(int x = 120; x < 180; ++x)
                {
                    d = (byte)(x - 120);
                    bbuffer[3 * x + stride * y] = 0;
                    bbuffer[3 * x + stride * y + 1] = 255;
                    bbuffer[3 * x + stride * y + 2] = (byte)(255 * d / 60);
                }

                for(int x = 180; x < 240; ++x)
                {
                    d = (byte)(x - 180);
                    bbuffer[3 * x + stride * y] = 0;
                    bbuffer[3 * x + stride * y + 1] = (byte)(255 - 255 * d / 60);
                    bbuffer[3 * x + stride * y + 2] = 255;
                }

                for(int x = 240; x < 300; ++x)
                {
                    d = (byte)(x - 240);
                    bbuffer[3 * x + stride * y] = (byte)(255 * d / 60);
                    bbuffer[3 * x + stride * y + 1] = 0;
                    bbuffer[3 * x + stride * y + 2] = 255;
                }

                for(int x = 300; x < 360; ++x)
                {
                    d = (byte)(x - 300);
                    bbuffer[3 * x + stride * y] = 255;
                    bbuffer[3 * x + stride * y + 1] = 0;
                    bbuffer[3 * x + stride * y + 2] = (byte)(255 - 255 * d / 60);
                }
            }
            _hueImage.WritePixels(new Int32Rect(0, 0, 360, h), bbuffer, 3 * 360, 0, 0);

            _huePicker.Source = _hueImage;
        }

        private void FillBrightnessSaturationField()
        {
            int h = 360;
            int w = 360;
            _bsImage = new WriteableBitmap(w, h, 96, 96,
                PixelFormats.Rgb24, BitmapPalettes.Halftone256);
            int stride = _bsImage.BackBufferStride;
            byte[] bbuffer = new byte[3 * (w + 1) * (h + 1)];

            // On X axis is saturation : color_sat = color_hue + s
            // On Y axis is brighness : color_final = color_sat * b
            // s = (255 - hue[ch])*ratio [x/w]

            double sR = ((double)(255 - _hue[R])) / (double)w;
            double sG = ((double)(255 - _hue[G])) / (double)w;
            double sB = ((double)(255 - _hue[B])) / (double)w;

            for(int y = 0; y < h; ++y)
            {
                double b = ((double)y) / ((double)h);
                for(int x = 0; x < w; ++x)
                {
                    bbuffer[3 * x + stride * y] = (byte)(Math.Min(255.0, _hue[R] + x * sR) * b);
                    bbuffer[3 * x + stride * y + 1] = (byte)(Math.Min(255.0, _hue[G] + x * sG) * b);
                    bbuffer[3 * x + stride * y + 2] = (byte)(Math.Min(255.0, _hue[B] + x * sB) * b);
                }
            }
            _bsImage.WritePixels(new Int32Rect(0, 0, 360, h), bbuffer, 3 * 360, 0, 0);

            _brightnessPicker.Source = _bsImage;
        }

        private void _tbBlue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!_colorChangedInternal)
            {
                _colorChangedInternal = true;
                Color c = PickedColor;
                c.B = (byte)_tbBlue.CurrentValue;
                UpdateRGB(c);
                _colorChangedInternal = false;
            }
        }

        private void _tbGreen_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!_colorChangedInternal)
            {
                _colorChangedInternal = true;
                Color c = PickedColor;
                c.G = (byte)_tbGreen.CurrentValue;
                UpdateRGB(c);
                _colorChangedInternal = false;
            }
        }

        private void _tbRed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!_colorChangedInternal)
            {
                _colorChangedInternal = true;
                Color c = PickedColor;
                c.R = (byte)_tbRed.CurrentValue;
                UpdateRGB(c);
                _colorChangedInternal = false;
            }
        }

        private void UpdateRGB(Color newColor)
        {
            PickedColor = newColor;
            UpdateHSB(newColor.R, newColor.G, newColor.B);
            FillBrightnessSaturationField();
            DrawColorPointers();
        }

        private void UpdateHSB(byte r, byte g, byte b)
        {
            if(r == g && r == b)
            {
                _brightness = r / 255.0;
                _saturation = 0.0;
                _hue[R] = 0;
                _hue[G] = 0;
                _hue[B] = 0;
                _hueAngle = 0.0;
                return;
            }

            byte[] rgb = new byte[3] { r, b, g };

            int[] sort;
            if(r < g && r < b)
            {
                if(g > b) sort = new int[3] { G, B, R };
                else sort = new int[3] { B, G, R };
            }
            else if(r > g && r > b)
            {
                if(g > b) sort = new int[3] { R, G, B };
                else sort = new int[3] { R, B, G };
            }
            else
            {
                if(g > b) sort = new int[3] { G, R, B };
                else sort = new int[3] { B, R, G };
            }

            _brightness = ((double)rgb[sort[0]]) / 255.0;
            _saturation = ((double)rgb[sort[2]]) / (_brightness*255.0);
            // color_sat = (255 - hue[ch])*ratio [x/w], color_sat = h + s = h + (255-h)*r =>
            // color_sat = h(1-r) + 255*r => h = (color_sat - 255*r)/(1-r) =>
            // color_final = rgb; color_sat = rgb / brigh;
            // for sort[2] : hue = 0 => s = color_sat = h + (255-h)*r = 255*r => r = color_sat / 255
            // So for sort[1] : hue = (rgb / brigh - 255*r)/(1-r) -> where r = _saturation

            byte mid = (byte)((rgb[sort[1]] / _brightness - 255.0*_saturation)/(1.0 - _saturation));

            _hue[sort[0]] = 255;
            _hue[sort[1]] = mid;
            _hue[sort[2]] = 0;

            if(sort[0] == R)
            {
                if(sort[1] == G) // ha in (0,60)
                    _hueAngle = 0.0 + 60.0 * _hue[sort[1]] / 255.0;
                else // ha in (300,360)
                    _hueAngle = 360.0 - 60.0 * _hue[sort[1]] / 255.0;
            }
            else if(sort[0] == G)
            {
                if(sort[1] == R) // ha in (60,120)
                    _hueAngle = 120.0 - 60.0 * _hue[sort[1]] / 255.0;
                else // ha in (120,180)
                    _hueAngle = 120.0 + 60.0 * _hue[sort[1]] / 255.0;
            }
            else
            {
                if(sort[1] == G) // ha in (180,240)
                    _hueAngle = 240.0 - 60.0 * _hue[sort[1]] / 255.0;
                else // ha in (240,300)
                    _hueAngle = 240.0 + 60.0 * _hue[sort[1]] / 255.0;
            }
        }

        private void UpdateHueFromAngle()
        {
            if(_hueAngle < 60.0)
            {
                _hue[R] = 255;
                _hue[G] = (byte)(255.0 * _hueAngle / 60.0);
                _hue[B] = 0;
            }
            else if(_hueAngle < 120.0)
            {
                _hue[R] = (byte)(255.0 - 255.0 * _hueAngle / 60.0);
                _hue[G] = 255;
                _hue[B] = 0;
            }
            else if(_hueAngle < 180.0)
            {
                _hue[R] = 0;
                _hue[G] = 255;
                _hue[B] = (byte)(255.0 * _hueAngle / 60.0);
            }
            else if(_hueAngle < 240.0)
            {
                _hue[R] = 0;
                _hue[G] = (byte)(255.0 - 255.0 * _hueAngle / 60.0);
                _hue[B] = 255;
            }
            else if(_hueAngle < 300.0)
            {
                _hue[R] = (byte)(255.0 * _hueAngle / 60.0);
                _hue[G] = 0;
                _hue[B] = 255;
            }
            else
            {
                _hue[R] = 255;
                _hue[G] = 0;
                _hue[B] = (byte)(255.0 - 255.0 * _hueAngle / 60.0);
            }
        }

        private void DrawColorPointers()
        {
            // Move color points
            double hueMargin = _huePointer_ZeroMargin + _huePointer_360Margin * _hueAngle;
            _huePointer.Margin = new Thickness(hueMargin, 0.0, 0.0, 0.0);

            double bsMarginX = _bsPointer_ZeroMargin[0] + _bsPointer_360Margin * _saturation;
            double bsMarginY = _bsPointer_ZeroMargin[1] + _bsPointer_360Margin * _brightness;
            _bsPointer.Margin = new Thickness(bsMarginX, bsMarginY, 0.0, 0.0);
            _bsPointer.BorderBrush = new SolidColorBrush(PickedColor.Invert());

            // Fill color preview with picked color
            _colorPreview.Fill = new SolidColorBrush(PickedColor);
        }

        private void _brightnessPicker_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                Point mpos = e.GetPosition(_brightnessPicker);
                PickSB(mpos);
            }
        }

        private void _brightnessPicker_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                Point mpos = e.GetPosition(_brightnessPicker);
                PickSB(mpos);
            }
        }

        private void _huePicker_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                Point mpos = e.GetPosition(_huePicker);
                PickHue(mpos);
            }
        }

        private void _huePicker_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                Point mpos = e.GetPosition(_huePicker);
                PickHue(mpos);
            }
        }

        private void PickHue(Point mpos)
        {
            double angle01 = mpos.X / _huePicker.Width;
            _hueAngle = 360.0 * angle01;

            UpdateHueFromAngle();
            FillBrightnessSaturationField();
            UpdateColorFromHSB();
            DrawColorPointers();
        }

        private void PickSB(Point mpos)
        {
            // We got b = Y scaled to [0-1]
            // And ratio x/w for saturation r = X scaled to [0-1]
            _saturation = mpos.X / _brightnessPicker.Width;
            _brightness = mpos.Y / _brightnessPicker.Height;
            UpdateColorFromHSB();
            DrawColorPointers();
        }

        private void UpdateColorFromHSB()
        {
            // Transform hue + sat + bright to color :
            // red = (byte)(Math.Min(255.0, _hue[R] + x * 255 - _hue[R] / w) * b); -> x/w = _saturation, b = _brightness
            Color color = new Color();
            color.A = 255;
            color.R = (byte)(Math.Min(255.0, _hue[R] + (255 - _hue[R]) * _saturation) * _brightness);
            color.G = (byte)(Math.Min(255.0, _hue[G] + (255 - _hue[G]) * _saturation) * _brightness);
            color.B = (byte)(Math.Min(255.0, _hue[B] + (255 - _hue[B]) * _saturation) * _brightness);
            
            _colorChangedInternal = true;
            PickedColor = color;
            _colorChangedInternal = false;
        }
    }
    
    public static class ColorInverter
    {
        public static Color Invert(this Color col)
        {
            Color inv = new Color();
            inv.A = col.A;
            inv.R = (byte)(255 - col.R);
            inv.G = (byte)(255 - col.G);
            inv.B = (byte)(255 - col.B);
            return inv;
        }
    }
}
