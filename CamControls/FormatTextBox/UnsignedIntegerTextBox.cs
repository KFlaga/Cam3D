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
    }
}
