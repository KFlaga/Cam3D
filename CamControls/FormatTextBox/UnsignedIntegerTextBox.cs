using System.Windows.Controls;

namespace CamControls
{
    public class UnsignedIntegerTextBox : NumberTextBox<uint>
    {
        public UnsignedIntegerTextBox()
        {
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            uint val;
            uint oldVal = _curVal;
            e.Handled = !uint.TryParse(this.Text, out val);
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
    }
}
