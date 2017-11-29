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
        private Dictionary<object, GuiModule> _modules;
        private GuiModule _currentModule = null;
        public double CalibResultsLeft
        {
            get
            {
                return _mainCanvas.ActualWidth / 2 - _calibResults.ActualWidth / 2;
            }
        }
        
        public MainWindow()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));

            InitializeComponent();

            _modules = new Dictionary<object, GuiModule>()
            {
                 { _headerCalibration, new CalibrationModule.Module() },
                 { _headerRectification, new RectificationModule.Module() },
                 { _headerMatching, new ImageMatchingModule.Module() },
                 { _headerTriangulation, new TriangulationModule.Module() },
                 { _headerImage3D, new Visualisation3dModule.Module() },
                 { _headerCapture, new CaptureModule.Module() },
                 { _headerOperations, new ImageOperationsModule.Module() },
            };

            InitCalibrationResultsAnimation();
            this.SizeChanged += (s, e) =>
            {
                Canvas.SetLeft(_calibResults, CalibResultsLeft);
            };

            this.Closed += (s, e) =>
            {
                App.Current.Shutdown();
            };
        }
        
        private void OpenModule(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            GuiModule module = _modules[item];
            OpenModule(module);
        }

        private void OpenModule(GuiModule module)
        {
            if(_currentModule != null)
            {
                if(!_currentModule.EndModule())
                {
                    MessageBox.Show("Cannot end this module right now: " + _currentModule.FailText);
                    return;
                }
            }
            _mainPanel.Children.Clear();
            _currentModule = module;
            if(!module.StartModule())
            {
                MessageBox.Show("Cannot start this module right now: " + module.FailText);
                return;
            }
            _mainPanel.Children.Add(module.MainPanel);
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ShowCalibrationData(object sender, RoutedEventArgs e)
        {
            _calibResults.Show();
        }

        private void InitCalibrationResultsAnimation()
        {
            _calibResults.Showing += (s, e) =>
            {
                Storyboard sbShow = (Storyboard)TryFindResource("calibResultShowAnimation");
                sbShow.SpeedRatio = -290 / (Canvas.GetTop(_calibResults) + 1);
                _calibResults.BeginStoryboard(sbShow);
            };
            _calibResults.Hiding += (s, e) =>
            {
                Storyboard sbHide = (Storyboard)TryFindResource("calibResultHideAnimation");
                sbHide.SpeedRatio = 290 / (Canvas.GetTop(_calibResults) + 291);
                _calibResults.BeginStoryboard(sbHide);
            };
        }

        private void LoadCalibrationData(object sender, RoutedEventArgs e)
        {
            FileOperations.LoadFromFile(
                (stream, path) => { CameraPair.Data.CopyFrom(XmlSerialisation.CreateFromFile<CameraPair>(stream)); }, 
                "Xml File|*.xml");
        }

        private void SaveCalibrationData(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(
                (stream, path) => { XmlSerialisation.SaveToFile(CameraPair.Data, stream); }, 
                "Xml File|*.xml");
        }

        private void StartChainProcess(object sender, RoutedEventArgs e)
        {
            ProcessingChain pc = new ProcessingChain();
            pc.Process();
        }
    }
}
