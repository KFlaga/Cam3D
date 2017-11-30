using CamCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CamAlgorithms;
using CamAlgorithms.Calibration;
using CamControls;

namespace RectificationModule
{
    class RectificationAlgorithmUi : IControllableAlgorithm
    {
        public string Name
        {
            get
            {
                return Algorithm.Name;
            }
        }

        public ImageRectification Algorithm { get; private set; } = new ImageRectification();
        
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
                { CurrentStatus = _status, OldStatus = old });
            }
        }
        public event EventHandler<CamCore.AlgorithmEventArgs> StatusChanged;

        public void Process()
        {
            Status = AlgorithmStatus.Running;
            Algorithm.ComputeRectificationMatrices();
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
            var window = new ParametersSelectionWindow();
            window.Processor = Algorithm;
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

            return result.ToString();
        }
    }
}
