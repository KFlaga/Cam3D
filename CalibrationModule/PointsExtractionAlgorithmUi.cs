using CamCore;
using System;
using System.Collections.Generic;
using System.Text;
using CamControls;
using CamAlgorithms.PointsExtraction;
using CamAlgorithms.Calibration;

namespace CalibrationModule
{
    class PointsExtractionAlgorithmUi : IControllableAlgorithm
    {
        public CalibrationPointsFinder Algorithm { get; set; }

        public string Name
        {
            get
            {
                return "Points Extraction";
            }
        }

        public IImage Image { get; set; }
        public List<CalibrationPoint> Points { get; protected set; }
        public List<List<Vector2>> CalibrationLines { get { return Algorithm?.LinesExtractor.CalibrationLines; } }

        public bool IsTerminable { get; } = false;
        public bool IsParametrizable { get; } = true;
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
                { CurrentStatus = _status, OldStatus = old, Algorithm = this });
            }
        }
        public event EventHandler<CamCore.AlgorithmEventArgs> StatusChanged;

        public void Process()
        {
            Status = AlgorithmStatus.Running;
            Algorithm.Image = Image;
            Algorithm.FindCalibrationPoints();
            Algorithm.LinesExtractor.ExtractLines();
            Points = Algorithm.Points;
            Status = AlgorithmStatus.Finished;
        }

        public string GetResults()
        {
            return PrepareResults();
        }

        public string GetProgress()
        {
            return "";
        }

        public void Terminate()
        {
            
        }

        public void ShowParametersWindow()
        {
            var window = new ParametrizableSelectionWindow();
            window.AddParametrizable(new ShapesGridCalibrationPointsFinder());
            window.ShowDialog();
            if(window.Accepted)
            {
                Algorithm = (CalibrationPointsFinder)window.Selected;
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
