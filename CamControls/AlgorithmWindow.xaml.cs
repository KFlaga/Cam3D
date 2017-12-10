using System;
using System.Threading.Tasks;
using System.Windows;
using CamCore;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;

namespace CamControls
{
    public partial class AlgorithmWindow : Window
    {
        AlgorithmTask _runAlgTask;

        DispatcherTimer _progressGuiUpdater = new DispatcherTimer();
        Stopwatch _executionTimeCounter = new Stopwatch();

        public IControllableAlgorithm Algorithm { get; private set; }

        public AlgorithmWindow(IControllableAlgorithm algorithm)
        {
            if(algorithm == null)
                throw new ArgumentNullException();

            Algorithm = algorithm;

            InitializeComponent();

            _buttonParams.IsEnabled = Algorithm.IsParametrizable;
            _buttonRun.IsEnabled = !Algorithm.IsParametrizable;

            this.Closed += (s, e) => { AbortTask(); };

            _progressGuiUpdater.Interval = TimeSpan.FromMilliseconds(1000.0);
            _progressGuiUpdater.Tick += _runningTimer_Tick;
            _labelAlgorithmTime.Content = "0";

            Algorithm.ParamtersAccepted += AlgorithmParamtersAccepted;
            Algorithm.StatusChanged += AlgorithmStatusChanged;

            _labelAlgorithmProgress.Content = "Not Run";
            
            _labelAlgorithmName.Content = Algorithm.Name;
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

        private void _buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            _textResults.Text = Algorithm.GetResults();
        }

        private void _buttonExit_Click(object sender, RoutedEventArgs e)
        {
            AbortTask();
            Close();
        }

        private void _buttonParams_Click(object sender, RoutedEventArgs e)
        {
            Algorithm.ShowParametersWindow();
        }

        private void AlgorithmStatusChanged(object sender, CamCore.AlgorithmEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if(e.CurrentStatus == AlgorithmStatus.Running)
                {
                    AlgorithmStarted();
                }
                else
                {
                    AlgorithmFinished();
                }
            });
        }

        void AbortTask()
        {
            if(_runAlgTask != null && Algorithm.Status == AlgorithmStatus.Running)
            {
                _runAlgTask.Abort();
            }
        }

        void RunAlgorithm()
        {
            _executionTimeCounter.Reset();
            _labelAlgorithmProgress.Content = "0";
            _labelAlgorithmTime.Content = "0";

            _runAlgTask = new AlgorithmTask() { Algorithm = Algorithm };
            _runAlgTask.Start();
        }

        void AlgorithmStarted()
        {
            _buttonAbort.IsEnabled = Algorithm.IsTerminable;
            _buttonRun.IsEnabled = false;
            _buttonRefresh.IsEnabled = true;
            _buttonParams.IsEnabled = false;

            _executionTimeCounter.Start();
            _progressGuiUpdater.Start();
            _labelAlgorithmStatus.Content = "Running";
        }

        private void AlgorithmFinished()
        {
            _progressGuiUpdater.Stop();
            _executionTimeCounter.Stop();
            _labelAlgorithmTime.Content = _executionTimeCounter.ElapsedMilliseconds.ToString() + "ms";
            _labelAlgorithmProgress.Content = Algorithm.GetProgress();
            
            if(_runAlgTask != null)
            {
                if(_runAlgTask.WasAborted)
                {
                    _textResults.Text = Algorithm.GetResults();
                    _labelAlgorithmStatus.Content = "Aborted";
                }
                else if(_runAlgTask.WasError)
                {
                    string text = "Algorithm failed. Error message: " + _runAlgTask.Error.Message;
                    text += "\r\nStack:\r\n" + _runAlgTask.Error.StackTrace + "\r\n";
                    text += "\r\nLast results:\r\n";
                    text += Algorithm.GetResults();
                    _textResults.Text = text;
                    _labelAlgorithmStatus.Content = "Error";
                }
                else
                {
                    _textResults.Text = Algorithm.GetResults();
                    _labelAlgorithmStatus.Content = "Finished";
                }
            }
            
            _buttonAbort.IsEnabled = false;
            _buttonRun.IsEnabled = true;
            _buttonRefresh.IsEnabled = false;
            _buttonParams.IsEnabled = Algorithm.IsParametrizable;
        }

        private void AlgorithmParamtersAccepted(object sender, EventArgs e)
        {
            _buttonRun.IsEnabled = true;
        }

        private void _runningTimer_Tick(object sender, EventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                _labelAlgorithmProgress.Content = this.Algorithm.GetProgress();
                _labelAlgorithmTime.Content = _executionTimeCounter.ElapsedMilliseconds.ToString() + "ms";
            }));
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
                    //try
                    //{
                        Algorithm.Process();
                    //}
                    //catch(Exception e)
                    //{
                     //   WasError = true;
                     //   Error = e;
                     //   Algorithm.Status = AlgorithmStatus.Error;
                    //}
                });
            }

            public void Abort()
            {
                if(Algorithm.IsTerminable)
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
