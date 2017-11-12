using System;
using System.Collections.Generic;

namespace CamCore
{
    public enum AlgorithmStatus
    {
        Idle,
        Running,
        Terminated,
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
        bool IsTerminable { get; }
        bool IsParametrizable { get; }

        AlgorithmStatus Status { get; set; }
        event EventHandler<AlgorithmEventArgs> StatusChanged;

        void Process();

        // If not supported, empty function should be set
        string GetResults();
        string GetProgress();
        void Terminate();

        void ShowParametersWindow();
        event EventHandler<EventArgs> ParamtersAccepted;
    }
}
