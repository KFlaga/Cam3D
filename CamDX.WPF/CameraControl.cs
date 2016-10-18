using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CamDX.WPF
{
    public class CameraControl
    {
        DX11Window _window;
        DX11Window Window
        {
            get { return _window; }
            set
            {
                if(_window != null)
                {
                    _window.KeyDown -= KeyPressed;
                    _window.KeyUp -= KeyReleased;
                    _window.MouseDown -= MousePressed;
                    _window.MouseUp -= MouseReleased;
                    _window.MouseMove -= MouseMove;
                    _window.MouseWheel -= MouseScroll;
                }
                _window = value;

                _window.KeyDown += KeyPressed;
                _window.KeyUp += KeyReleased;
                _window.MouseDown += MousePressed;
                _window.MouseUp += MouseReleased;
                _window.MouseMove += MouseMove;
                _window.MouseWheel += MouseScroll;
            }
        }

        DXCamera Camera { get; set; }

        public CameraControl()
        {

        }

        public void KeyPressed(object sender, KeyEventArgs e)
        {

        }

        public void KeyReleased(object sender, KeyEventArgs e)
        {

        }

        public void MousePressed(object sender, MouseButtonEventArgs e)
        {

        }

        public void MouseReleased(object sender, MouseButtonEventArgs e)
        {

        }

        public void MouseMove(object sender, MouseEventArgs e)
        {

        }

        public void MouseScroll(object sender, MouseWheelEventArgs e)
        {

        }
    }
}
