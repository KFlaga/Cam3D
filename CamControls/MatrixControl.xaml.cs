using MathNet.Numerics.LinearAlgebra;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace CamControls
{
    /// <summary>
    /// Control that enables showing / editing Matrix
    /// </summary>
    public partial class MatrixControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty MatrixSourceProperty =
            DependencyProperty.Register("MatrixSource", typeof(Matrix<double>), typeof(MatrixControl), new PropertyMetadata()
            {
                DefaultValue = null,
                PropertyChangedCallback = OnMatrixSourceChanged
            });
        public Matrix<double> MatrixSource
        {
            get
            {
                return this.GetValue(MatrixSourceProperty) as Matrix<double>;
            }
            set
            {
                this.SetValue(MatrixSourceProperty, value);
            }
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(MatrixControl), new PropertyMetadata()
            {
                DefaultValue = true,
                PropertyChangedCallback = OnIsReadOnlyChanged
            });
        public bool IsReadOnly
        {
            get
            {
                return (bool)this.GetValue(IsReadOnlyProperty);
            }
            set
            {
                this.SetValue(IsReadOnlyProperty, value);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private static void OnMatrixSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            MatrixControl matCtrl = obj as MatrixControl;
            matCtrl.UpdateMatrix();
            if (matCtrl.PropertyChanged != null)
            {
                matCtrl.PropertyChanged(matCtrl, new PropertyChangedEventArgs("MatrixSource"));
            }
        }

        private static void OnIsReadOnlyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            MatrixControl matCtrl = obj as MatrixControl;
            if (matCtrl.PropertyChanged != null)
            {
                matCtrl.PropertyChanged(matCtrl, new PropertyChangedEventArgs("IsReadOnly"));
            }
            foreach(var cell in matCtrl._mainGrid.Children)
            {
                ((SingleTextBox)cell).IsReadOnly = (bool)e.NewValue;
            }
        }

        // Creates grid definition for matrix and fills each cell with
        // textbox with matrix value
        private void UpdateMatrix()
        {
            _mainGrid.Children.Clear();
            _mainGrid.ColumnDefinitions.Clear();
            _mainGrid.RowDefinitions.Clear();

            if (MatrixSource == null)
                return;

            // Add Columns/Rows definitions -> equal width/height
            // Total size is defined externally
            for (int c = 0; c < MatrixSource.ColumnCount; c++)
            {
                ColumnDefinition column = new ColumnDefinition();
                column.Width = new GridLength(10, GridUnitType.Star);
                _mainGrid.ColumnDefinitions.Add(column);
            }

            for (int r = 0; r < MatrixSource.RowCount; r++)
            {
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(10, GridUnitType.Star);
                _mainGrid.RowDefinitions.Add(row);
            }

            // Fill cells with textboxes
            for (int r = 0; r < MatrixSource.RowCount; r++)
                for (int c = 0; c < MatrixSource.ColumnCount; c++)
                {
                    DoubleTextBox cell = new DoubleTextBox();
                    cell.LimitValue = false;
                    cell.SetNumber(double.Parse(MatrixSource[r, c].ToString("F4")));
                    Grid.SetColumn(cell, c);
                    Grid.SetRow(cell, r);
                    cell.VerticalContentAlignment = VerticalAlignment.Center;
                    cell.HorizontalContentAlignment = HorizontalAlignment.Center;
                    _mainGrid.Children.Add(cell);

                    cell.IsReadOnly = IsReadOnly; 
                    cell.TextChanged += (s, e) =>
                    {
                        MatrixSource[Grid.GetRow(cell), Grid.GetColumn(cell)] = (double)cell.CurrentValue;
                    };
                }
        }

        public MatrixControl()
        {
            IsReadOnly = true;
            InitializeComponent();
        }
    }
}
