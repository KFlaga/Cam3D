using System;
using System.Collections.Generic;
using System.Text;
using CamCore;
using CamAlgorithms.Calibration;
using CamAlgorithms.Triangulation;
using CamControls;

namespace TriangulationModule
{
    public class TriangulationAlgorithmUi : IControllableAlgorithm
    {
        public TriangulationAlgorithm Algorithm { get; set; } = new TriangulationAlgorithm();

        public List<TriangulatedPoint> Points { get { return Algorithm.Points; } set { Algorithm.Points = value; } }
        public CameraPair Cameras { get { return Algorithm.Cameras; } set { Algorithm.Cameras = value; } }
        public bool Recitifed { get { return Algorithm.Recitifed; } set { Algorithm.Recitifed = value; } }

        public string Name { get { return Algorithm.Name; } }
        
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
                StatusChanged?.Invoke(this, new AlgorithmEventArgs()
                { CurrentStatus = _status, OldStatus = old });
            }
        }
        public event EventHandler<AlgorithmEventArgs> StatusChanged;
        
        public void Process()
        {
            Status = AlgorithmStatus.Running;
            if(Cameras == null) { Cameras = CameraPair.Data; }
            Algorithm.Find3DPoints();
            Status = AlgorithmStatus.Finished;
        }

        public string GetResults()
        {
            return PrepareResults();
        }

        public string GetProgress()
        {
            return "Point " + Algorithm.CurrentPoint.ToString() +
                " of " + Algorithm.Points.Count.ToString();
        }

        public void Terminate()
        {
            Algorithm.Terminate();
        }

        public void ShowParametersWindow()
        {
            var window = new ParametersSelectionWindow()
            {
                Processor = Algorithm
            };
            window.ShowDialog();
            if(window.Accepted)
            {
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

            result.AppendLine();

            return result.ToString();
        }
    }
}


