using CamAlgorithms;
using CamAlgorithms.Calibration;
using CamCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace CamMain
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private List<Module> _modules;
        private Dictionary<object, Module> _modules;
        private Module _currentModule = null;
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

            //_modules = new List<Module>();
            //var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            //string dir = System.IO.Path.GetDirectoryName(entryAssembly.Location);
            //LoadModules(dir + "\\config.xml");

            _modules = new Dictionary<object, Module>()
            {
                 { _headerCalibration, new CalibrationModule.CalibModule() },
                 { _headerRectification, new RectificationModule.RectModule() },
                 { _headerMatching, new ImageMatchingModule.MatchingModule() },
                 { _headerTriangulation, new TriangulationModule.TriModule() },
                 { _headerImage3D, new Image3DModule.Image3DConstructionModule() },
                 { _headerCapture, new CaptureModule.CamCaptureModule() },
                 { _headerOperations, new CamImageOperationsModule.ImageOperationsModule() },
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
            Module module = _modules[item];
            OpenModule(module);
        }

        private void OpenModule(Module module)
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
            FileOperations.LoadFromFile(CameraPair.Data.LoadFromFile, "Xml File|*.xml");
        }

        private void SaveCalibrationData(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(CameraPair.Data.SaveToFile, "Xml File|*.xml");
        }

        private void StartChainProcess(object sender, RoutedEventArgs e)
        {
            ProcessingChain1 pc = new ProcessingChain1();
            pc.Process();
        }
        

        //private void LoadModules(string file)
        //{
        //    CamCore.ModuleLoader moduleLoader = new CamCore.ModuleLoader();
        //    moduleLoader.ConfFilePath = file;
        //    moduleLoader.LoadModules();
        //    _modules = moduleLoader.Modules;

        //    foreach(Module module in _modules)
        //    {
        //        MenuItem submenu = new MenuItem();
        //        submenu.Header = module.Name;
        //        submenu.Click += (s, ee) =>
        //        {
        //            if(_currentModule != null)
        //                if(!_currentModule.EndModule())
        //                {
        //                    MessageBox.Show("Cannot end this module right now: " + _currentModule.FailText);
        //                    return;
        //                }
        //            _mainPanel.Children.Clear();
        //            _currentModule = module;
        //            if(!module.StartModule())
        //            {
        //                MessageBox.Show("Cannot start this module right now: " + module.FailText);
        //                return;
        //            }
        //            _mainPanel.Children.Add(module.MainPanel);
        //        };
        //        _menuModules.Items.Add(submenu);
        //    }
        //}

        //private void UnloadModules()
        //{
        //    if(_currentModule != null)
        //        _currentModule.EndModule();
        //    _mainPanel.Children.Clear();
        //    _menuModules.Items.Clear();
        //    foreach(Module module in _modules)
        //    {
        //        module.Dispose();
        //    }
        //    _modules.Clear();
        //}

        //private void ReloadModules(object sender, RoutedEventArgs e)
        //{
        //    UnloadModules();
        //    LoadModules("d:\\config.xml");
        //}
    }
}
