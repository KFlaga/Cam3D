using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CamAlgorithms.ImageMatching;
using System.ComponentModel;

namespace ImageMatchingModule
{
    public partial class RefinerChooseWindow : Window
    {
        List<DisparityRefinement> _availableRefiners = new List<DisparityRefinement>();
        DisparityRefinement _choosenRefiner;
        bool _changeInternal = false;
        public DisparityRefinement ChoosenRefiner
        {
            get
            {
                return _choosenRefiner;
            }

            set
            {
                _changeInternal = true;
                foreach(var refiner in _availableRefiners)
                {
                    if(refiner.GetType() == value.GetType())
                    {
                        _refinersCombo.SelectedItem = refiner;

                        refiner.InitParameters();
                        for(int i = 0; i < refiner.Parameters.Count; ++i)
                        {
                            refiner.Parameters[i].ActualValue = value.Parameters[i].ActualValue;
                        }
                        refiner.UpdateParameters();

                        _paramsPanel.SetParameters(refiner.Parameters);
                        _choosenRefiner = refiner;
                    }
                }
                _changeInternal = false;
            }
        }

        public bool Accepted { get; private set; }
        public bool RemoveCurrent { get; private set; }

        public RefinerChooseWindow()
        {
            InitializeComponent();
            
            // Add and init all available refiners
            MedianFilterRefiner medianRefiner = new MedianFilterRefiner();
            medianRefiner.InitParameters();
            _availableRefiners.Add(medianRefiner);

            PeakRemovalRefiner peakRefiner = new PeakRemovalRefiner();
            peakRefiner.InitParameters();
            _availableRefiners.Add(peakRefiner);

            CrossCheckRefiner crossRefiner = new CrossCheckRefiner();
            crossRefiner.InitParameters();
            _availableRefiners.Add(crossRefiner);

            LimitRangeRefiner limitRefiner = new LimitRangeRefiner();
            limitRefiner.InitParameters();
            _availableRefiners.Add(limitRefiner);

            InvalidateLowConfidenceRefiner confidenceRefiner = new InvalidateLowConfidenceRefiner();
            confidenceRefiner.InitParameters();
            _availableRefiners.Add(confidenceRefiner);

            AnisotopicDiffusionRefiner diffusionRefiner = new AnisotopicDiffusionRefiner();
            diffusionRefiner.InitParameters();
            _availableRefiners.Add(diffusionRefiner);

            SmoothSegmentsRefiner smoothRefiner = new SmoothSegmentsRefiner();
            smoothRefiner.InitParameters();
            _availableRefiners.Add(smoothRefiner);

            _refinersCombo.Items.Add(medianRefiner);
            _refinersCombo.Items.Add(peakRefiner);
            _refinersCombo.Items.Add(crossRefiner);
            _refinersCombo.Items.Add(limitRefiner);
            _refinersCombo.Items.Add(confidenceRefiner);
            _refinersCombo.Items.Add(diffusionRefiner);
            _refinersCombo.Items.Add(smoothRefiner);
        }

        private void _refinersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(_changeInternal == false && e.AddedItems.Count == 1)
            {
                _choosenRefiner = e.AddedItems[0] as DisparityRefinement;
                _paramsPanel.SetParameters(_choosenRefiner.Parameters);
            }
        }

        private void _butRemove_Click(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            RemoveCurrent = true;
            
            Close();
        }

        private void _butCancel_Click(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            RemoveCurrent = false;

            Close();
        }

        private void _butAccept_Click(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            RemoveCurrent = false;

            if(ChoosenRefiner != null)
            {
                ChoosenRefiner.UpdateParameters();
            }

            Close();
        }
    }
}
