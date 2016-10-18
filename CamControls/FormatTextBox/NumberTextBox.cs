using System;
using System.Windows;
using System.Windows.Controls;

namespace CamControls
{
    public abstract class NumberTextBox : TextBox
    {
        public abstract object GetNumber();
        public abstract void SetNumber(object num);
        public abstract void SetMinMaxValues(object min, object max);
    }

    public class NumberTextBox<T> : NumberTextBox where T : IComparable<T>, IConvertible, IFormattable
    {
        protected T _curVal;
        
        public T MaxValue { get; set; }
        public T MinValue { get; set; }
        public bool LimitValue { get; set; }
        
        public virtual T CurrentValue
        {
            get
            {
                return _isEmpty ? default(T) : _curVal;
            }
            set
            {
                bool changed = !_curVal.Equals(value);
                _curVal = value;
                if(changed)
                    this.Text = _curVal.ToString();

                ValueChanged?.Invoke(this, new NumberTextBoxValueChangedEventArgs<T>()
                {
                    NewValue = _curVal
                });
            }
        }

        protected bool _isEmpty = true;

        public NumberTextBox()
        {

        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if(!e.Handled)
            {
                if(LimitValue)
                {
                    if(_curVal.CompareTo(MaxValue) > 0)
                    {
                        _curVal = MaxValue;
                        e.Handled = true;
                        Text = _curVal.ToString();
                    }
                    else if(_curVal.CompareTo(MinValue) < 0)
                    {
                        _curVal = MinValue;
                        e.Handled = true;
                        Text = _curVal.ToString();
                    }
                }

                _isEmpty = false;
                CurrentValue = _curVal;
            }

            base.OnTextChanged(e);
        }

        public override object GetNumber()
        {
            return CurrentValue;
        }

        public override void SetNumber(object num)
        {
            CurrentValue = (T)num;
        }

        public override void SetMinMaxValues(object min, object max)
        {
            LimitValue = true;
            MinValue = (T)min;
            MaxValue = (T)max;
        }

        public event EventHandler<NumberTextBoxValueChangedEventArgs<T>> ValueChanged;
        public void ClearValueChangedEvent()
        {
            ValueChanged = null;
        }
    }

    public class NumberTextBoxValueChangedEventArgs<T> : EventArgs
    {
        public T NewValue { get;set; }
    }
}
