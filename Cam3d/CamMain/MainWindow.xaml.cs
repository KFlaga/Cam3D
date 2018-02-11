using CamAlgorithms.Calibration;
using CamAutomatization;
using CamCore;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace CamMain
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));

            InitializeComponent();
            
            this.Closed += (s, e) =>
            {
                App.Current.Shutdown();
            };
        }

        private void StartChainProcess(object sender, RoutedEventArgs e)
        {
            ProcessingChain pc = new ProcessingChain();
            pc.Process();
        }
    }
}
