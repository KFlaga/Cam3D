
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace CamControls
{
    public class DoubleTextBox : NumberTextBox<double>
    {
        public int Precision { get; set; }

        public DoubleTextBox()
        {
            Precision = 2;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            double val;
            double oldVal = _curVal;

            e.Handled = !double.TryParse(this.Text, out val);

            if(!e.Handled)
                _curVal = val;
            else if(Text.Length == 0) // Empty text -> allow
                _isEmpty = true;
            else if(_isEmpty) // Bad value entered, but previously was empty, so leave it
                Text = "";
            else
                Text = oldVal.ToString();

            base.OnTextChanged(e);
        }

        public override void SetNumber(object num)
        {
            CurrentValue = (double)num;
        }

        public override void SetMinMaxValues(object min, object max)
        {
            LimitValue = true;
            MinValue = (double)min;
            MaxValue = (double)max;
        }
    }

    public class SingleTextBox : NumberTextBox<float>
    {
        public int Precision { get; set; }

        public SingleTextBox()
        {
            Precision = 2;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            float val;
            float oldVal = _curVal;

            e.Handled = !float.TryParse(this.Text, out val);

            if(!e.Handled)
                _curVal = val;
            else if(Text.Length == 0) // Empty text -> allow
                _isEmpty = true;
            else if(_isEmpty) // Bad value entered, but previously was empty, so leave it
                Text = "";
            else
                Text = oldVal.ToString();

            base.OnTextChanged(e);
        }

        public override void SetNumber(object num)
        {
            CurrentValue = (float)num;
        }

        public override void SetMinMaxValues(object min, object max)
        {
            LimitValue = true;
            MinValue = (float)min;
            MaxValue = (float)max;
        }
    }
}
