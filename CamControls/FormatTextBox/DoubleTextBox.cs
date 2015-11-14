
using System.Windows.Controls;

namespace CamControls
{
    public class DoubleTextBox : NumberTextBox<double>
    {
        public int Precision { get; set; }
        private bool texthandled;
        private int caretPos;

        public DoubleTextBox()
        {
            Precision = 2;
            texthandled = false;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if(texthandled)
            {
                this.CaretIndex = caretPos;
                texthandled = false;
                return;
            }
            texthandled = true;
            caretPos = this.CaretIndex;

            double val;
            double oldVal = _curVal;
            e.Handled = !double.TryParse(this.Text, out val);
            if(!e.Handled)
                _curVal = val;
            else
                Text = oldVal.ToString("F"+Precision.ToString());

            _valInRange = false;

            base.OnTextChanged(e);

            if(_valInRange)
            {
                _curVal = val;
                Text = _curVal.ToString("F" + Precision.ToString());
            }
            else
            {
                _curVal = oldVal;
                Text = oldVal.ToString("F" + Precision.ToString());
            }
        }
    }

    public class SingleTextBox : NumberTextBox<float>
    {
        public int Precision { get; set; }
        private bool texthandled;
        private int caretPos;

        public SingleTextBox()
        {
            Precision = 2;
            texthandled = false;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (texthandled)
            {
                this.CaretIndex = caretPos;
                texthandled = false;
                return;
            }
            texthandled = true;
            caretPos = this.CaretIndex;

            float val;
            float oldVal = _curVal;
            e.Handled = !float.TryParse(this.Text, out val);
            if (!e.Handled)
                _curVal = val;
            else
                Text = oldVal.ToString();

            _valInRange = false;

            base.OnTextChanged(e);

            if (_valInRange)
            {
                _curVal = val;
                Text = _curVal.ToString("F" + Precision.ToString());
            }
            else
            {
                _curVal = oldVal;
                Text = oldVal.ToString();
            }
        }
    }
}
