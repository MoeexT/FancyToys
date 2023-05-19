using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;

using Windows.ApplicationModel.DataTransfer;

using MemoryPack;

using FancyToys.Annotations;
using FancyToys.Controls.Dialogs;
using FancyToys.Logging;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys.Service.Nursery {

    [MemoryPackable]
    // ReSharper disable once PartialTypeWithSinglePart
    public partial struct NurseryItemStruct {
        public int NurseryId;
        public string Alias;
        public string FilePath;
        public string Arguments;
        public bool RedirectIOE;
        public bool IsAlive;

        public override string ToString() {
            return $"{{" +
                $"NurseryId: {NurseryId}, " +
                $"Alias: {Alias}, " +
                $"FilePath: {FilePath} ," +
                $"Arguments: {Arguments}, " +
                $"RedirectIOE: {RedirectIOE}, " +
                $"IsAlive: {IsAlive}" +
                $"}}";
        }
    }

    public sealed partial class NurseryItem: INotifyPropertyChanged {

        public delegate void ProcessInfoHandler(NurseryItem ps);

        public event ProcessInfoHandler OnProcessExited;
        public event ProcessInfoHandler OnProcessLaunched;

        // public event ProcessInfoHandler OnProcessAdd;
        public event ProcessInfoHandler OnItemDeleted;

        public event PropertyChangedEventHandler PropertyChanged;

        public int NurseryId { get; private set; }
        public string Alias { get; private set; }
        public string FilePath { get; set; }
        public string Arguments { get; set; }
        public bool RedirectIOE { get; private set; }

        public bool IsAlive {
            get => _isAlive;
            private set {
                // process is already running or stopped
                if (value == _isAlive) {
                    Dogger.Error($"Invalid process state, value: {value}");
                    return;
                }
                _isAlive = value;

                if (value) {
                    Task.Run(Launch);
                } else {
                    Stop();
                }
            }
        }

        public string SwitchContent => $"{Alias} is {(IsAlive ? "running" : "stopped")}";

        private bool _isAlive;
        private readonly bool _dissociative;
        private readonly object _launchLock;

        private Process _nurseryProcess;
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _memCounter;

        public NurseryItem(Process nurseryProcess) {
            _isAlive = true;
            _dissociative = true;
            _launchLock = new object();
            NurseryId = nurseryProcess.Id;
            _nurseryProcess = nurseryProcess;
            Alias = nurseryProcess.ProcessName;
            _nurseryProcess.Exited += OnProcessOnExited;
            _nurseryProcess.EnableRaisingEvents = true;
            _cpuCounter = new PerformanceCounter("Process", "% Processor Time", _nurseryProcess.ProcessName);
            _memCounter = new PerformanceCounter("Process", "Working Set - Private", _nurseryProcess.ProcessName);

            InitializeComponent();
        }

        public NurseryItem(string pathName, string arguments) {
            FilePath = pathName;
            Arguments = arguments;
            Alias = Path.GetFileName(FilePath);
            _launchLock = new object();
            InitializeProcess();

            InitializeComponent();
        }

        public NurseryItemStruct GetStruct() {
            return new NurseryItemStruct() {
                NurseryId = NurseryId,
                Alias = Alias,
                FilePath = FilePath,
                Arguments = Arguments,
                RedirectIOE = RedirectIOE,
                IsAlive = IsAlive,
            };
        }

        public ProcessStatistic Statistic() {
            try {
                return new ProcessStatistic(NurseryId,
                    _nurseryProcess.Id,
                    _nurseryProcess.ProcessName,
                    _cpuCounter.NextValue(),
                    _memCounter.NextValue());
            } catch (Exception e) {
                Dogger.Error(e.ToString());
                return new ProcessStatistic(NurseryId, 0, Alias, 0, 0);
            }
        }

        /// <summary>
        /// Launch the process
        /// </summary>
        /// <returns></returns>
        private void Launch() {
            Dogger.Trace($"Launching {Alias}");

            // if this process had been redirected std-ioe, cancel first
            if (RedirectIOE) {
                try {
                    _nurseryProcess.CancelOutputRead();
                    _nurseryProcess.CancelErrorRead();
                } catch (Exception e) {
                    Dogger.Error(e.Message);
                }
                RedirectIOE = false;
            }

            lock (_launchLock) {
                bool launchSucceed = _nurseryProcess.Start();

                if (!launchSucceed) { // launch failed
                    Dogger.Error($"Process launch failed: {Alias}");
                    return;
                }
                _isAlive = true;
                DispatcherQueue.TryEnqueue(() => OnPropertyChanged(nameof(SwitchContent)));
            }

            // TODO InvalidOperationException: process has exited.
            if (!_nurseryProcess.HasExited) {
                NurseryId = _nurseryProcess.Id;
                Alias = _nurseryProcess.ProcessName;
                _cpuCounter = new PerformanceCounter("Process", "% Processor Time", _nurseryProcess.ProcessName);
                _memCounter = new PerformanceCounter("Process", "Working Set - Private", _nurseryProcess.ProcessName);
                OnProcessLaunched?.Invoke(this);
                Dogger.Info($"Process {_nurseryProcess.ProcessName}[{_nurseryProcess.Id}] launched successfully.");
            }

            // TODO System.InvalidOperationException:“An async read operation has already been started on the stream.”

            if (!RedirectIOE) {
                _nurseryProcess.BeginOutputReadLine();
                _nurseryProcess.BeginErrorReadLine();
                RedirectIOE = true;
            }
        }

        /// <summary>
        /// Stop the process.
        /// </summary>
        public void Stop() {
            _nurseryProcess.Kill();
            Dogger.Info("Process killed.");
        }

        public async Task<bool> Delete() {
            bool confirm = true;

            if (IsAlive) {
                confirm &= await MessageDialog.Warn("进程未退出", "继续操作可能丢失工作内容", "仍然退出");
            }

            if (!confirm) {
                return false;
            }

            // dispose the process instance
            Dispose();
            OnItemDeleted?.Invoke(this);

            return true;
        }

        /// <summary>
        /// Stop the process and dispose the resource. 
        /// </summary>
        private void Dispose() {
            if (IsAlive) {
                _nurseryProcess.Kill();
            }
            _nurseryProcess.Dispose();
            Dogger.Trace($"Process disposed: {NurseryId}, {Alias}");
        }

        /// <summary>
        /// add a original process and init its start info or something else
        /// </summary>
        private void InitializeProcess() {
            _nurseryProcess = new Process();
            _nurseryProcess.EnableRaisingEvents = true;
            _nurseryProcess.StartInfo.FileName = FilePath;
            _nurseryProcess.StartInfo.CreateNoWindow = true;
            _nurseryProcess.StartInfo.UseShellExecute = false;
            _nurseryProcess.StartInfo.RedirectStandardError = true;
            _nurseryProcess.StartInfo.RedirectStandardOutput = true;
            _nurseryProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(FilePath) ?? string.Empty;
            _nurseryProcess.Exited += OnProcessOnExited;
            _nurseryProcess.ErrorDataReceived += (s, e) => Dogger.StdError(((Process)s).Id, e.Data);
            _nurseryProcess.OutputDataReceived += (s, e) => Dogger.StdOutput(((Process)s).Id, e.Data);
            Dogger.Trace($"Process created with file: {FilePath}");
        }

        private async void ArgsFlyoutItem_OnClick(object sender, RoutedEventArgs e) {
            if (IsAlive) {
                Dogger.Warn("Running process cannot attach arguments.");
                return;
            }
            
            InputDialog inputDialog = new("Nursery", "输入参数", 
                string.IsNullOrEmpty(Arguments) ? _nurseryProcess.StartInfo.Arguments : Arguments) {
                XamlRoot = XamlRoot,
            };
            // Value does not fall within the expected range
            await inputDialog.ShowAsync();

            if (inputDialog.isSaved) {
                Arguments = inputDialog.inputContent;
                _nurseryProcess.StartInfo.Arguments = inputDialog.inputContent;
            }
        }

        private void CopyPathFlyoutItem_OnClick(object sender, RoutedEventArgs e) {
            DataPackage dataPackage = new();
            dataPackage.SetText(FilePath);
            Clipboard.SetContent(dataPackage);
        }

        private async void DeleteFlyoutItem_OnClick(object sender, RoutedEventArgs e) {
            await Delete();
        }

        private void OnProcessOnExited(object sender, EventArgs _) {
            lock (_launchLock) {
                _isAlive = false;
                DispatcherQueue.TryEnqueue(() => {
                    OnPropertyChanged(nameof(IsAlive));
                    OnPropertyChanged(nameof(SwitchContent));
                });
            }

            OnProcessExited?.Invoke(this);
            Dogger.Trace("Process exited." + Alias);

            // caught process
            if (_dissociative) {
                if (string.IsNullOrEmpty(FilePath)) {
                    Dogger.Error($"Caught process {Alias} cannot be initialized.");
                    return;
                }
                InitializeProcess();
            }
        }

        public override string ToString() =>
            $"<ProcessInfo>{{Id: {NurseryId}, IsAlive: {IsAlive}, Alias: {Alias}}}";

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}
