using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;

using FancyToys.Annotations;
using FancyToys.Controls.Dialogs;
using FancyToys.Logging;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace FancyToys.Service.Nursery {

    public sealed partial class NurseryItem: UserControl, INotifyPropertyChanged {

        public delegate void ProcessInfoHandler(NurseryItem ps);

        public event ProcessInfoHandler OnProcessExited;
        public event ProcessInfoHandler OnProcessLaunched;

        // public event ProcessInfoHandler OnProcessAdd;
        // public event ProcessInfoHandler OnProcessRemoved;

        public event PropertyChangedEventHandler PropertyChanged;

        private Process NurseryProcess { get; set; }
        public readonly int NurseryId;
        public string FilePath { get; private set; }
        private string Alias { get; set; }

        public bool IsAlive {
            get => _isAlive;
            private set {
                _isAlive = value;

                DispatcherQueue.TryEnqueue(() => {
                    OnPropertyChanged();
                });
            }
        }

        public bool AutoRestart { get; private set; }
        public bool RedirectIOE { get; private set; }
        private PerformanceCounter CpuCounter { get; set; }
        private PerformanceCounter MemCounter { get; set; }
        public string SwitchOnContent { get => $"{Alias} is running"; }
        public string SwitchOffContent { get => $"{Alias} is stopped"; }

        private byte _restartCount;
        private readonly object _launchLock;
        private bool _isAlive;
        private static int _idCursor;

        private NurseryItem(Process nurseryProcess) {
            _launchLock = null;
            NurseryId = _idCursor++;
            NurseryProcess = nurseryProcess;
            AutoRestart = false;
            Alias = nurseryProcess.ProcessName;
            NurseryProcess.Exited += OnProcessOnExited;
            NurseryProcess.EnableRaisingEvents = true;
            CpuCounter = new PerformanceCounter("Process", "% Processor Time", NurseryProcess.ProcessName);
            MemCounter = new PerformanceCounter("Process", "Working Set - Private", NurseryProcess.ProcessName);

            InitializeComponent();
        }

        private NurseryItem(string pathName) {
            NurseryId = _idCursor++;
            FilePath = pathName;
            Alias = Path.GetFileName(FilePath);
            _launchLock = new object();
            InitializeProcess(FilePath);

            InitializeComponent();
        }

        public static bool WithPath(string pathName, out NurseryItem item) {
            if (!File.Exists(pathName)) {
                Dogger.Error("File not exist.");
                item = null;
                return false;
            }

            item = new NurseryItem(pathName);
            return true;
        }

        public static bool WithProcess(Process process, out NurseryItem item) {
            if (process is null) {
                Dogger.Error("Null or dead process.");
                item = null;
                return false;
            }

            item = new NurseryItem(process);

            return true;
        }

        /// <summary>
        /// Launch the process
        /// </summary>
        /// <returns></returns>
        private bool Launch() {
            Dogger.Trace($"Launching {Alias}");

            // caught process
            if (_launchLock is null) {
                Dogger.Error($"Caught process ${Alias} cannot be launched.");
                return false;
            }

            // TODO _launchLock 锁NurseryProcess

            // process is already running
            if (IsAlive) {
                Dogger.Error($"Process {Alias} is running.");
                return false;
            }


            // if this process had been redirected std-ioe, cancel first
            if (RedirectIOE) {
                NurseryProcess.CancelOutputRead();
                NurseryProcess.CancelErrorRead();
                RedirectIOE = false;
            }

            lock (_launchLock) {
                bool launchSucceed = NurseryProcess.Start();
                _isAlive = true;

                if (!launchSucceed) { // launch failed
                    Dogger.Error($"Process launch failed: {Alias}");
                    return false;
                }
            }

            // TODO InvalidOperationException: process has exited.
            if (!NurseryProcess.HasExited) {
                CpuCounter = new PerformanceCounter("Process", "% Processor Time", NurseryProcess.ProcessName);
                MemCounter = new PerformanceCounter("Process", "Working Set - Private", NurseryProcess.ProcessName);
                Alias = NurseryProcess.ProcessName;
                OnProcessLaunched?.Invoke(this);
                Dogger.Info($"Process {NurseryProcess.ProcessName}[{NurseryProcess.Id}] launched successfully.");
            }

            // TODO System.InvalidOperationException:“An async read operation has already been started on the stream.”
            _restartCount = 0;

            if (RedirectIOE) {
                Dogger.Info($"Process has been redirected output stream.");
                return true;
            }

            NurseryProcess.BeginOutputReadLine();
            NurseryProcess.BeginErrorReadLine();
            RedirectIOE = true;

            return true;
        }

        /// <summary>
        /// Stop the process.
        /// </summary>
        public void Stop() {
            if (IsAlive) {
                NurseryProcess.Kill();
                Dogger.Info("Process killed.");
            } else {
                Dogger.Warn($"Process {Alias}({NurseryId}) already exited.");
            }
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
            return true;
        }

        /// <summary>
        /// Stop the process and dispose the resource. 
        /// </summary>
        private void Dispose() {
            if (IsAlive) {
                NurseryProcess.Kill();
            }
            NurseryProcess.Dispose();
            Dogger.Trace($"Process disposed: {NurseryId}, {Alias}");
        }

        /// <summary>
        /// add a original process and init its start info or something else
        /// </summary>
        /// <param name="pathName">file's full name with path</param>
        private void InitializeProcess(string pathName) {
            NurseryProcess = new Process();
            NurseryProcess.EnableRaisingEvents = true;
            NurseryProcess.StartInfo.FileName = pathName;
            NurseryProcess.StartInfo.CreateNoWindow = true;
            NurseryProcess.StartInfo.UseShellExecute = false;
            NurseryProcess.StartInfo.RedirectStandardError = true;
            NurseryProcess.StartInfo.RedirectStandardOutput = true;
            NurseryProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(pathName) ?? string.Empty;
            NurseryProcess.Exited += OnProcessOnExited;

            NurseryProcess.OutputDataReceived += (s, e) => {
                if (!string.IsNullOrEmpty(e.Data)) Dogger.StdOutput(((Process)s).Id, e.Data);
            };

            NurseryProcess.ErrorDataReceived += (s, e) => {
                if (!string.IsNullOrEmpty(e.Data)) Dogger.StdError(((Process)s).Id, e.Data);
            };
            Dogger.Trace($"Process created with file: {pathName}");
        }

        public ProcessStatistic Statistic() {
            try {
                return new ProcessStatistic(NurseryId,
                    NurseryProcess.Id,
                    NurseryProcess.ProcessName,
                    CpuCounter.NextValue(),
                    MemCounter.NextValue());
            } catch (Exception e) {
                Dogger.Error(e.ToString());
                return new ProcessStatistic(NurseryId, 0, Alias, 0, 0);
            }
        }

        private async void ArgsFlyoutItem_OnClick(object sender, RoutedEventArgs e) {
            if (sender is not MenuFlyoutItem ai) {
                Dogger.Error("args-button is null");
                return;
            }

            int pid = (int)ai.Tag;

            InputDialog inputDialog = new("Nursery", "输入参数", NurseryProcess.StartInfo.Arguments) {
                XamlRoot = XamlRoot,
            };
            // Value does not fall within the expected range
            await inputDialog.ShowAsync();

            if (inputDialog.isSaved) {
                NurseryProcess.StartInfo.Arguments = inputDialog.inputContent;
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
            Dogger.Trace("Process exited." + Alias);

            if (_launchLock is null) {
                return;
            }

            if (AutoRestart && _restartCount < 3) {
                _restartCount++;

                Dogger.Info(NurseryProcess.Start()
                    ? $"Restart {NurseryProcess.ProcessName}({NurseryId}) successfully."
                    : $"Restart {NurseryProcess.ProcessName}({NurseryId}) failed.");
            } else {
                lock (_launchLock) {
                    IsAlive = false;
                }
                OnProcessExited?.Invoke(this);
                Dogger.Info($"Process {Alias} exited.{(_restartCount > 0 ? $" (Restarted {_restartCount} times)" : "")}");
            }
        }

        private void NurseryToggleSwitch_OnToggled(object sender, RoutedEventArgs e) {
            if (sender is not ToggleSwitch ts) {
                return;
            }

            if (ts.IsOn) {
                ts.IsOn = Launch();
            } else {
                Stop();
            }
        }

        public override string ToString() =>
            $"<ProcessInfo>{{Id:{NurseryId}, IsAlive:{IsAlive}, AutoRestart:{AutoRestart}, Alias:{Alias}}}";

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}
