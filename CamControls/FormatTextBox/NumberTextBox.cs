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
        public static DependencyProperty CurrentValueProperty =
            DependencyProperty.Register("CurrentValue", typeof(T), typeof(NumberTextBox<T>));
        public static DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(T), typeof(NumberTextBox<T>));
        public static DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(T), typeof(NumberTextBox<T>));
        public static DependencyProperty LimitValueProperty =
            DependencyProperty.Register("LimitValue", typeof(bool), typeof(NumberTextBox<T>));

        protected T _curVal;

        public T MaxValue
        {
            get { return (T)GetValue(MaxValueProperty); }
            set
            {
                SetValue(MaxValueProperty, value);
            }
        }
        public T MinValue
        {
            get { return (T)GetValue(MinValueProperty); }
            set
            {
                SetValue(MinValueProperty, value);
            }
        }
        public bool LimitValue
        {
            get { return (bool)GetValue(LimitValueProperty); }
            set { SetValue(LimitValueProperty, value); }
        }
        public virtual T CurrentValue
        {
            get
            {
                return _isEmpty ? default(T) : _curVal;
            }
            set
            {
                _curVal = value;
                SetValue(CurrentValueProperty, value);
                this.Text = _curVal.ToString();
            }
        }

        protected bool _isEmpty = true;

        public NumberTextBox()
        {
            
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!e.Handled)
            {
                if (LimitValue)
                {
                    if (CurrentValue.CompareTo(MaxValue) > 0 )
                    {
                        _curVal = MaxValue;
                        Text = _curVal.ToString();
                    }
                    else if(CurrentValue.CompareTo(MinValue) < 0)
                    {
                        _curVal = MinValue;
                        Text = _curVal.ToString();
                    }
                }
                
               _isEmpty = false;
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
    }
}
