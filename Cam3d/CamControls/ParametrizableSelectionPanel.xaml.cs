using CamCore;
using System.Windows;
using System.Windows.Controls;

namespace CamControls
{
    public partial class ParametrizableSelectionPanel : DockPanel
    {
        public string NameOfParametrizable { set { NameLabel.Content = "Choose " + value; } }
        public ComboBox ParametrizablesCombo { get; set; }
        public Label NameLabel { get; set; }
        public ParametersSelectionPanel ParametersPanel { get; set; }

        public IParameterizable Selected { get; set; }
        
        public ParametrizableSelectionPanel()
        {
            InitializeComponent();
            InitParametrizebleList();
        }

        public void InitParametrizebleList()
        {
            NameLabel = new Label();
            NameLabel.Content = "Choose Parametrizable";
            DockPanel.SetDock(NameLabel, Dock.Top);
            NameLabel.Height = 30.0;
            NameLabel.HorizontalContentAlignment = HorizontalAlignment.Center;
            this.Children.Add(NameLabel);

            ParametrizablesCombo = new ComboBox();
            DockPanel.SetDock(ParametrizablesCombo, Dock.Top);
            ParametrizablesCombo.Height = 25.0;
            ParametrizablesCombo.Margin = new Thickness(30.0, 5.0, 30.0, 5.0);
            ParametrizablesCombo.HorizontalContentAlignment = HorizontalAlignment.Center;
            this.Children.Add(ParametrizablesCombo);

            ParametrizablesCombo.SelectionChanged += ComboProcessors_SelectionChanged;

            ParametersPanel = new ParametersSelectionPanel();
            ParametersPanel.MinHeight = 20.0;
            DockPanel.SetDock(ParametersPanel, Dock.Top);
            this.Children.Add(ParametersPanel);
        }

        private void ComboProcessors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Selected = (IParameterizable)e.AddedItems[0];
            ParametersPanel.SetParameters(Selected.Parameters);
        }

        public void AddParametrizable(IParameterizable processor)
        {
            processor.InitParameters();
            ParametrizablesCombo.Items.Add(processor);
        }
    }
}
