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
