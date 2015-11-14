using System.Windows.Controls;

namespace CamControls
{
    public class IntegerTextBox : NumberTextBox<int>
    {
        public IntegerTextBox()
        {

        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            int val;
            int oldVal = _curVal;
            e.Handled = !int.TryParse(this.Text, out val);
            if (!e.Handled)
                _curVal = val;
            else
                Text = oldVal.ToString();

            _valInRange = false;

            base.OnTextChanged(e);

            if (_valInRange)
            {
                _curVal = val;
            }
            else
            {
                _curVal = oldVal;
                Text = oldVal.ToString();
            }
        }

        public override void SetNumber(object num)
        {
            CurrentValue = (int)num;
        }

        public override void SetMinMaxValues(object min, object max)
        {
            LimitValue = true;
            MinValue = (int)min;
            MaxValue = (int)max;
        }
    }
}
