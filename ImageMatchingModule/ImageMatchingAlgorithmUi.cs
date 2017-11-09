﻿using System;
using System.Text;
using CamCore;
using CamAlgorithms.ImageMatching;
using CamControls;

namespace ImageMatchingModule
{
    public class ImageMatchingAlgorithmUi : IControllableAlgorithm
    {
        public ImageMatchingAlgorithm Algorithm { get; set; }

        public IImage ImageLeft { get; set; }
        public IImage ImageRight { get; set; }

        public DisparityMap MapLeft { get { return Algorithm.MapLeft; } }
        public DisparityMap MapRight { get { return Algorithm.MapRight; } }

        public string Name { get { return "Dense Image Matching"; } }

        public bool SupportsTermination { get; } = true;
        public bool SupportsParameters { get; } = true;
        public event EventHandler<EventArgs> ParamtersAccepted;

        private AlgorithmStatus _status = AlgorithmStatus.Idle;
        public AlgorithmStatus Status
        {
            get { return _status; }
            set
            {
                AlgorithmStatus old = _status;
                _status = value;
                StatusChanged?.Invoke(this, new CamCore.AlgorithmEventArgs()
                { CurrentStatus = _status, OldStatus = old });
            }
        }
        public event EventHandler<CamCore.AlgorithmEventArgs> StatusChanged;
        
        public void Process()
        {
            Status = AlgorithmStatus.Running;
            Algorithm.ImageLeft = ImageLeft;
            Algorithm.ImageRight = ImageRight;
            Algorithm.MatchImages();
            Status = AlgorithmStatus.Finished;
        }

        public string GetResults()
        {
            return PrepareResults();
        }

        public string GetProgress()
        {
            return Algorithm.GetProgress();
        }
        
        public void Terminate()
        {
            Algorithm.Terminate();
        }

        public void ShowParametersWindow()
        {
            var window = new ParametrizedProcessorsSelectionWindow();
            window.AddProcessorFamily("Image Matching Algorithm");
            window.AddToFamily("Image Matching Algorithm", new CppSgmMatchingAlgorithm());
            window.AddToFamily("Image Matching Algorithm", new GenericImageMatchingAlgorithm());
            window.ShowDialog();
            if(window.Accepted)
            {
                Algorithm = (ImageMatchingAlgorithm)window.GetSelectedProcessor("Image Matching Algorithm");
                ParamtersAccepted?.Invoke(this, new EventArgs());
            }
        }

        private string PrepareResults()
        {
            StringBuilder result = new StringBuilder();
            result.Append("State: ");

            if(Status == AlgorithmStatus.Finished)
                result.Append("Finished");
            else if(Status != AlgorithmStatus.Error)
                result.Append("Not Finished");
            else
                result.Append("Error");
            
            return result.ToString();
        }
    }
}

