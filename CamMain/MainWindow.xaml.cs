﻿using CamCore;
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
        private List<Module> _modules;
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
            InitializeComponent();
            _modules = new List<Module>();
            LoadModules("d:\\config.xml");

            InitCalibrationResultsAnimation();
            this.SizeChanged += (s, e) =>
            {
                Canvas.SetLeft(_calibResults, CalibResultsLeft);
            };
        }

        private void LoadModules(string file)
        {
            CamCore.ModuleLoader moduleLoader = new CamCore.ModuleLoader();
            moduleLoader.ConfFilePath = file;
            moduleLoader.LoadModules();
            _modules = moduleLoader.Modules;

            foreach (Module module in _modules)
            {
                MenuItem submenu = new MenuItem();
                submenu.Header = module.Name;
                submenu.Click += (s, ee) =>
                {
                    if(_currentModule != null)
                        if(!_currentModule.EndModule())
                        {
                            MessageBox.Show("Cannot end this module right now: " + _currentModule.FailText);
                            return;
                        }
                    _mainPanel.Children.Clear();
                    _currentModule = module;
                    if(!module.StartModule())
                    {
                        MessageBox.Show("Cannot start this module right now: " + module.FailText);
                        return;
                    }
                    _mainPanel.Children.Add(module.MainPanel);
                };
                _menuModules.Items.Add(submenu);
            }
        }

        private void UnloadModules()
        {
            if (_currentModule != null)
                _currentModule.EndModule();
            _mainPanel.Children.Clear();
            _menuModules.Items.Clear();
            foreach (Module module in _modules)
            {  
                module.Dispose();
            }
            _modules.Clear();
        }

        private void ReloadModules(object sender, RoutedEventArgs e)
        {
            UnloadModules();
            LoadModules("d:\\config.xml");
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
                sbShow.SpeedRatio = -290 / (Canvas.GetTop(_calibResults)+1);
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
            FileOperations.LoadFromFile(CalibrationData.Data.LoadFromFile, "Xml File|*.xml");
        }

        private void SaveCalibrationData(object sender, RoutedEventArgs e)
        {
            FileOperations.SaveToFile(CalibrationData.Data.SaveToFile, "Xml File|*.xml");
        }
    }
}
