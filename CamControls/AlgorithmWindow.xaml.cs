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

namespace CamControls
{
    public partial class AlgorithmWindow : Window
    {
        IControllableAlgorithm _alg;
        AlgorithmStatus Status { get; set; } = AlgorithmStatus.Idle;

        AlgorithmTask _runAlgTask;

        DispatcherTimer _runningTimer = new DispatcherTimer();
        int _runTime;

        public IControllableAlgorithm Algorithm
        {
            get { return _alg; }
        }

        public AlgorithmWindow(IControllableAlgorithm algorithm)
        {
            if(algorithm == null)
                throw new ArgumentNullException();

            _alg = algorithm;

            InitializeComponent();
            
            _buttonParams.IsEnabled = _alg.SupportsParameters;
            _buttonRun.IsEnabled = !_alg.SupportsParameters;

            this.Closed += (s, e) => { AbortTask(); };

            _runningTimer.Interval = TimeSpan.FromMilliseconds(1000.0);
            _runningTimer.Tick += _runningTimer_Tick;

            _alg.ParamtersAccepted += _algParamtersAccepted;
        }

        private void _buttonRun_Click(object sender, RoutedEventArgs e)
        {
            RunAlgorithm();
        }

        private void _buttonAbort_Click(object sender, RoutedEventArgs e)
        {
            AbortTask();
            
            _buttonSuspend.IsEnabled = false;
            _buttonAbort.IsEnabled = false;
            _buttonRun.IsEnabled = true;
            _buttonResume.IsEnabled = false;
            _buttonRefresh.IsEnabled = false;
            _buttonParams.IsEnabled = _alg.SupportsParameters;
        }

        private void _buttonSuspend_Click(object sender, RoutedEventArgs e)
        {
            _alg.Suspend();
            Status = _alg.Status;

            if(Status == AlgorithmStatus.Suspended)
            {
                _buttonSuspend.IsEnabled = false;
                _buttonAbort.IsEnabled = true;
                _buttonRun.IsEnabled = false;
                _buttonResume.IsEnabled = true;
                _buttonRefresh.IsEnabled = _alg.SupportsPartialResults;
                _buttonParams.IsEnabled = false;

                _runningTimer.Stop();
            }
        }

        private void _buttonResume_Click(object sender, RoutedEventArgs e)
        {
            _alg.Resume();
            Status = _alg.Status;

            if(Status == AlgorithmStatus.Running)
            {
                _buttonSuspend.IsEnabled = true;
                _buttonAbort.IsEnabled = true;
                _buttonRun.IsEnabled = false;
                _buttonResume.IsEnabled = false;
                _buttonRefresh.IsEnabled = _alg.SupportsPartialResults;
                _buttonParams.IsEnabled = false;

                _runningTimer.Start();
            }
        }

        private void _buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            _textResults.Text = _alg.GetPartialResults();
        }

        private void _buttonExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void _buttonParams_Click(object sender, RoutedEventArgs e)
        {
            _alg.ShowParametersWindow();
        }

        void AbortTask()
        {
            if(_runAlgTask != null && 
                (Status == AlgorithmStatus.Running ||
                Status == AlgorithmStatus.Suspended))
            {
                _runAlgTask.Abort();
            }
        }

        async void RunAlgorithm()
        {
            _runAlgTask = new AlgorithmTask() { Algorithm = _alg };
            _runAlgTask.OnFinished += AlgorithmTaskFinished;
            await new Task(_runAlgTask.Start);

            Status = _alg.Status;

            if(Status == AlgorithmStatus.Running)
            {
                _buttonAbort.IsEnabled = true;
                _buttonParams.IsEnabled = false;
                _buttonRefresh.IsEnabled = _alg.SupportsPartialResults;
                _buttonResume.IsEnabled = false;
                _buttonRun.IsEnabled = false;
                _buttonSuspend.IsEnabled = _alg.SupportsSuspension;

                _runTime = 0;
                _runningTimer.Start();
            }
        }

        private void _runningTimer_Tick(object sender, EventArgs e)
        {
            _runTime += 1;
            if(_alg.Status != AlgorithmStatus.Running)
            {
                // ?? Something failed
                throw new Exception();
            }

            Dispatcher.Invoke(() =>
            {
                if(_alg.SupportsProgress)
                    _labelAlgorithmProgress.Content = _alg.GetProgress();
                _labelAlgorithmTime.Content = _runTime;
            });
        }

        private void AlgorithmTaskFinished(object sender, EventArgs e)
        {
            _runningTimer.Stop();

            AlgorithmTask task = sender as AlgorithmTask;
            if(task.WasAborted)
            {
                if(_alg.SupportsPartialResults)
                {
                    Dispatcher.Invoke(() =>
                   {
                       _textResults.Text = _alg.GetPartialResults();
                   });
                }
            }
            else if(task.WasError)
            {
                string text = "Algorithm failed. Error message: " + task.Error.Message;
                Dispatcher.Invoke(() =>
                {
                    _textResults.Text = text;
                });
            }
            else
            {
                if(_alg.SupportsFinalResults)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _textResults.Text = _alg.GetFinalResults();
                    });
                }
            }
        }

        private void _algParamtersAccepted(object sender, EventArgs e)
        {
            _buttonRun.IsEnabled = true;
        }

        class AlgorithmTask
        {
            public IControllableAlgorithm Algorithm { get; set; }
            public Task Worker { get; private set; }
            public bool WasAborted { get; private set; }

            public bool WasError { get; private set; }
            public Exception Error { get; set; }

            public event EventHandler<EventArgs> OnFinished;

            private CancellationTokenSource _canceller;
            private bool _started;

            class TaskAbortedException : Exception { }
            
            async public void Start()
            {
                WasAborted = false;
                WasError = false;
                _started = false;
                _canceller = new CancellationTokenSource();

                Worker = Task.Run(() =>
                {
                    try
                    {
                        using(_canceller.Token.Register(
                           () => { throw new TaskAbortedException(); }))
                        {
                            _started = true;
                            Algorithm.Process();
                        }
                    }
                    catch(TaskAbortedException)
                    {
                        WasAborted = true;
                        if(Algorithm.SupportsTermination)
                            Algorithm.Terminate();
                    }
                    catch(Exception e)
                    {
                        WasError = true;
                        Error = e;
                        if(Algorithm.SupportsTermination)
                            Algorithm.Terminate();
                    }
                    OnFinished?.Invoke(this, new EventArgs());
                }, _canceller.Token);

                await Task.Run(() =>
                {
                    int timeLeft = 1000;
                    while(_started == false)
                    {
                        Task.Delay(1);
                        timeLeft--;
                        if(timeLeft <= 0)
                            return;
                    }
                });
            }

            public void Abort()
            {
                _canceller.Cancel();
            }
        }
    }
}
