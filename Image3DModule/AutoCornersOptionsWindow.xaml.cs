using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CamImageProcessing;

namespace Image3DModule
{
    public partial class AutoCornersOptionsWindow : Window
    {
        public bool Accepted { get; set; }
        public FeaturesDetector SelectedDetector { get; private set; }
        public ImagesMatcher SelectedMatcher { get; private set; }

        private List<FeaturesDetector> _detectors;
        private List<ImagesMatcher> _matchers;

        public AutoCornersOptionsWindow()
        {
            InitializeComponent();
            Accepted = false;

            InitLists();

            _cbDetector.SelectionChanged += _cbDetector_SelectionChanged;
            _cbMatcher.SelectionChanged += _cbMatcher_SelectionChanged;
        }

        private void _cbDetector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedDetector = (FeaturesDetector)e.AddedItems[0];
            _panelDetectorOptions.Children.Clear();
            foreach (ProcessorParameter parameter in SelectedDetector.Parameters)
            {
                _panelDetectorOptions.Children.Add(CreateOptionSelector(parameter));
            }
        }

        private void _cbMatcher_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedMatcher = (ImagesMatcher)e.AddedItems[0];
            _panelMatcherOptions.Children.Clear();
            foreach (ProcessorParameter parameter in SelectedMatcher.Parameters)
            {
                _panelMatcherOptions.Children.Add(CreateOptionSelector(parameter));
            }
        }

        private void InitLists()
        {
            _detectors = new List<FeaturesDetector>();

            FeaturesDetector detectorSusan = new FeatureSUSANDetector();
            _cbDetector.Items.Add(detectorSusan);
            _detectors.Add(detectorSusan);

            _matchers = new List<ImagesMatcher>();

            ImagesMatcher matcherLoGCorr = new LoGCorrelationFeaturesMatcher();
            _cbMatcher.Items.Add(matcherLoGCorr);
            _matchers.Add(matcherLoGCorr);

            ImagesMatcher matcherArea = new AreaBasedCorrelationImageMatcher();
            _cbMatcher.Items.Add(matcherArea);
            _matchers.Add(matcherArea);
        }
        
        private UIElement CreateOptionSelector(ProcessorParameter parameter)
        {
            DockPanel optPanel = new DockPanel();
            optPanel.Height = 25;

            Label name = new Label();
            name.Content = parameter.Name;
            DockPanel.SetDock(name, Dock.Left);

            if (parameter.TypeName.Contains("Boolean"))
            {
                CheckBox checkBox = new CheckBox();
                checkBox.IsChecked = (bool)parameter.DefaultValue;
                checkBox.Checked += (s, e) => { parameter.ActualValue = true; };
                checkBox.Unchecked += (s, e) => { parameter.ActualValue = false; };
                checkBox.HorizontalAlignment = HorizontalAlignment.Center;
                checkBox.HorizontalContentAlignment = HorizontalAlignment.Center;
                checkBox.VerticalAlignment = VerticalAlignment.Center;

                DockPanel.SetDock(checkBox, Dock.Right);
                optPanel.Children.Add(checkBox);
            }
            else
            {
                CamControls.NumberTextBox textBox = null;

                if (parameter.TypeName.Contains("UInt"))
                {
                    textBox = new CamControls.UnsignedIntegerTextBox();
                }
                else if (parameter.TypeName.Contains("Int"))
                {
                    textBox = new CamControls.IntegerTextBox();
                }
                else if (parameter.TypeName.Contains("Single"))
                {
                    textBox = new CamControls.SingleTextBox();
                }
                else if (parameter.TypeName.Contains("Double"))
                {
                    textBox = new CamControls.DoubleTextBox();
                }

                textBox.SetNumber(parameter.DefaultValue);
                textBox.SetMinMaxValues(parameter.MinValue, parameter.MaxValue);
                textBox.MinWidth = 100;
                textBox.HorizontalAlignment = HorizontalAlignment.Center;
                textBox.HorizontalContentAlignment = HorizontalAlignment.Center;
                textBox.VerticalAlignment = VerticalAlignment.Center;
                DockPanel.SetDock(textBox, Dock.Right);
                optPanel.Children.Add(textBox);

                textBox.TextChanged += (s, e) =>
                {
                    parameter.ActualValue = textBox.GetNumber();
                };
            }

            optPanel.Children.Add(name);

            return optPanel;
        }

        public void Accept(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            Close();
        }

        public void Cancel(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            Close();
        }
    }
}
