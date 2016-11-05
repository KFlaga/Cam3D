using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CamCore;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;

namespace CamControls
{
    public partial class AlgorithmWindow : Window
    {
        IControllableAlgorithm _alg;

        AlgorithmTask _runAlgTask;

        DispatcherTimer _progresTimer = new DispatcherTimer();
        Stopwatch _runTimeTimer = new Stopwatch();

        public IControllableAlgorithm Algorithm
        {
            get { return _alg; }
        }

        //public event EventHandler<AlgorithmEventArgs> AlgorithmResultsAccepted;

        public AlgorithmWindow(IControllableAlgorithm algorithm)
        {
            if(algorithm == null)
                throw new ArgumentNullException();

            _alg = algorithm;

            InitializeComponent();

            _buttonParams.IsEnabled = _alg.SupportsParameters;
            _buttonRun.IsEnabled = !_alg.SupportsParameters;

            this.Closed += (s, e) => { AbortTask(); };

            _progresTimer.Interval = TimeSpan.FromMilliseconds(1000.0);
            _progresTimer.Tick += _runningTimer_Tick;
            _labelAlgorithmTime.Content = "0";

            _alg.ParamtersAccepted += AlgorithmParamtersAccepted;
            _alg.StatusChanged += AlgorithmStatusChanged;

            if(_alg.SupportsProgress)
                _labelAlgorithmProgress.Content = "Not Run";
            else
                _labelAlgorithmProgress.Content = "Not Supported";

            _labelAlgorithmName.Content = _alg.Name;
            _labelAlgorithmStatus.Content = "Waiting";
        }

        private void _buttonRun_Click(object sender, RoutedEventArgs e)
        {
            RunAlgorithm();
        }

        private void _buttonAbort_Click(object sender, RoutedEventArgs e)
        {
            AbortTask();
        }

        private void _buttonSuspend_Click(object sender, RoutedEventArgs e)
        {
            _alg.Suspend();
        }

        private void _buttonResume_Click(object sender, RoutedEventArgs e)
        {
            _alg.Resume();
        }

        private void _buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            _textResults.Text = _alg.GetPartialResults();
        }

        private void _buttonExit_Click(object sender, RoutedEventArgs e)
        {
            AbortTask();
            Close();
        }

        private void _buttonParams_Click(object sender, RoutedEventArgs e)
        {
            _alg.ShowParametersWindow();
        }
        
        //private void _buttonAcceptResults_Click(object sender, RoutedEventArgs e)
        //{
        //    AlgorithmResultsAccepted?.Invoke(this, new AlgorithmEventArgs()
        //    {
        //        Algorithm = _alg,
        //        CurrentStatus = _alg.Status,
        //        OldStatus = _alg.Status,
        //    });
        //}

        private void AlgorithmStatusChanged(object sender, CamCore.AlgorithmEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if(e.CurrentStatus == AlgorithmStatus.Running)
                {
                    AlgorithmStarted();
                }
                else if(e.CurrentStatus == AlgorithmStatus.Suspended)
                {
                    AlgorithmSuspended();
                }
                else
                {
                    AlgorithmFinished();
                }
            });
        }

        void AbortTask()
        {
            if(_runAlgTask != null &&
                (_alg.Status == AlgorithmStatus.Running ||
                _alg.Status == AlgorithmStatus.Suspended))
            {
                _runAlgTask.Abort();
            }
        }

        void RunAlgorithm()
        {
            _runTimeTimer.Reset();
            if(_alg.SupportsProgress)
            {
                _labelAlgorithmProgress.Content = "0";
                _labelAlgorithmTime.Content = "0";
            }

            _runAlgTask = new AlgorithmTask() { Algorithm = _alg };
            _runAlgTask.Start();
        }

        void AlgorithmStarted()
        {
            _buttonSuspend.IsEnabled = Algorithm.SupportsSuspension;
            _buttonAbort.IsEnabled = Algorithm.SupportsTermination;
            _buttonRun.IsEnabled = false;
            _buttonResume.IsEnabled = false;
            _buttonRefresh.IsEnabled = _alg.SupportsPartialResults;
            _buttonParams.IsEnabled = false;

            _runTimeTimer.Start();
            _progresTimer.Start();
            _labelAlgorithmStatus.Content = "Running";
        }

        void AlgorithmSuspended()
        {
            _buttonSuspend.IsEnabled = false;
            _buttonAbort.IsEnabled = Algorithm.SupportsTermination;
            _buttonRun.IsEnabled = false;
            _buttonResume.IsEnabled = true;
            _buttonRefresh.IsEnabled = _alg.SupportsPartialResults;
            _buttonParams.IsEnabled = false;

            _runTimeTimer.Stop();
            _progresTimer.Stop();
            _labelAlgorithmStatus.Content = "Suspended";
        }

        private void AlgorithmFinished()
        {
            _progresTimer.Stop();
            _runTimeTimer.Stop();
            _labelAlgorithmTime.Content = _runTimeTimer.ElapsedMilliseconds.ToString() + "ms";
            if(_alg.SupportsProgress)
                _labelAlgorithmProgress.Content = _alg.GetProgress();

            if(_runAlgTask != null)
            {
                if(_runAlgTask.WasAborted)
                {
                    if(_alg.SupportsPartialResults)
                    {
                        _textResults.Text = _alg.GetPartialResults();
                    }
                    _labelAlgorithmStatus.Content = "Aborted";
                }
                else if(_runAlgTask.WasError)
                {
                    string text = "Algorithm failed. Error message: " + _runAlgTask.Error.Message;
                    if(_alg.SupportsPartialResults)
                    {
                        text += "\r\nLast results:\r\n";
                        text += _alg.GetPartialResults();
                    }
                    _textResults.Text = text;
                    _labelAlgorithmStatus.Content = "Error";
                }
                else
                {
                    if(_alg.SupportsFinalResults)
                    {
                        _textResults.Text = _alg.GetFinalResults();
                    }
                    _labelAlgorithmStatus.Content = "Finished";
                }
            }

            _buttonSuspend.IsEnabled = false;
            _buttonAbort.IsEnabled = false;
            _buttonRun.IsEnabled = true;
            _buttonResume.IsEnabled = false;
            _buttonRefresh.IsEnabled = false;
            _buttonParams.IsEnabled = _alg.SupportsParameters;
        }

        private void AlgorithmParamtersAccepted(object sender, EventArgs e)
        {
            _buttonRun.IsEnabled = true;
        }

        private void _runningTimer_Tick(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if(_alg.SupportsProgress)
                    _labelAlgorithmProgress.Content = _alg.GetProgress();
                _labelAlgorithmTime.Content = _runTimeTimer.ElapsedMilliseconds.ToString() + "ms";
            });
        }

        class AlgorithmTask
        {
            public IControllableAlgorithm Algorithm { get; set; }
            public Task Worker { get; private set; }
            public bool WasAborted { get; private set; }

            public bool WasError { get; private set; }
            public Exception Error { get; set; }

            public void Start()
            {
                WasAborted = false;
                WasError = false;
                
                Worker = Task.Run(() =>
                {
                    try
                    {
                        Algorithm.Process();
                    }
                    catch(Exception e)
                    {
                        WasError = true;
                        Error = e;
                        Algorithm.Status = AlgorithmStatus.Error;
                    }
                });
            }

            public void Abort()
            {
                if(Algorithm.SupportsTermination)
                {
                    WasAborted = true;
                    Algorithm.Terminate();
                }
            }
        }

        private void _buttonSave_Click(object sender, RoutedEventArgs e)
        {
            CamCore.FileOperations.SaveToFile(SaveToFile, "AllFiles|*.*");
        }

        public void SaveToFile(Stream file, string path)
        {
            StreamWriter writer = new StreamWriter(file);

            string text = _textResults.Text;
            for(int c = 0; c < text.Length; ++c)
            {
                writer.Write(text[c]);
            }

            writer.Close();
        }
    }
}
