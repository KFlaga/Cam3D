using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CamControls
{
    public class PositiveDoubleTextBox : TextBox
    {
        public PositiveDoubleTextBox()
        {
        }

        protected override void OnPreviewTextInput(System.Windows.Input.TextCompositionEventArgs e)
        {
            if (e.Text == ",")
            {
                e.Handled = Text.Contains(',');
            }
            else
            {
                Regex regex = new Regex("[^0-9]+"); //regex that matches disallowed text
                e.Handled = regex.IsMatch(e.Text);
            }
            base.OnPreviewTextInput(e);
        }
        
    }
}
