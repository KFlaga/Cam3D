using CamAlgorithms.Calibration;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CamMain
{
    public partial class CalibrationResults : UserControl
    {
        public CalibrationResults()
        {
            InitializeComponent();
            this.DataContext = CameraPair.Data;
        }

        public void Show()
        {
            if (Showing != null)
                Showing(this, new RoutedEventArgs());
        }

        public void Hide()
        {
            if (Hiding != null)
                Hiding(this, new RoutedEventArgs());
        }

        public EventHandler<RoutedEventArgs> Showing;
        public EventHandler<RoutedEventArgs> Hiding;

        private void SlideButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
