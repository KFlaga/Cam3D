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
        Waiting,
        Running,
        Terminated,
        Suspended,
        Finished,
        Error
    }

    public interface IControllableAlgorithm
    {
        string Name { get; }

        bool SupportsFinalResults { get; }
        bool SupportsPartialResults { get; }

        bool SupportsProgress { get; }
        bool SupportsSuspension { get; }
        bool SupportsTermination { get; }

        bool SupportsParameters { get; }
        AlgorithmStatus Status { get; }

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
