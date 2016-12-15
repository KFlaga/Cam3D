using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamCore
{
    public enum AlgorithmStatus
    {
        Idle,
        Running,
        Terminated,
        Suspended,
        Finished,
        Error
    }

    public class AlgorithmEventArgs : EventArgs
    {
        public IControllableAlgorithm Algorithm;
        public AlgorithmStatus OldStatus;
        public AlgorithmStatus CurrentStatus;
    }

    public interface IControllableAlgorithm : INamed
    {
        bool SupportsFinalResults { get; }
        bool SupportsPartialResults { get; }

        bool SupportsProgress { get; }
        bool SupportsSuspension { get; }
        bool SupportsTermination { get; }

        bool SupportsParameters { get; }
        AlgorithmStatus Status { get; set; }
        event EventHandler<AlgorithmEventArgs> StatusChanged;

        void Process();

        // If not supported, empty function should be set
        string GetFinalResults();
        string GetPartialResults();
        string GetProgress();
        void Suspend();
        void Resume();
        void Terminate();

        void ShowParametersWindow();
        event EventHandler<EventArgs> ParamtersAccepted;
    }
}
