using System;
using System.Diagnostics;
using System.IO;

using Microsoft.UI.Xaml.Controls;

using FancyToys.Logging;


namespace FancyToys.Nursery {

    public class NurseryItem {
        public delegate void ProcessInfoHandler(NurseryItem ps);

        // public event ProcessInfoHandler OnProcessAdd;
        // public event ProcessInfoHandler OnProcessLaunched;
        public event ProcessInfoHandler OnProcessExited;
        // public event ProcessInfoHandler OnProcessRemoved;

        public readonly int NurseryId;
        public bool IsRunning { get; set; }

        // public bool StopByServer;
        public bool AutoRestart { get; set; }
        public bool RedirectIoe { get; set; }

        private byte _restartCount;
        public string Alias { get; set; } // process name, after process launched
        public readonly Process Ps;
        public PerformanceCounter CpuCounter;
        public PerformanceCounter MemCounter;

        private readonly object _launchLock;
        private readonly int _idCursor;

        public ToggleSwitch Switch { get; set; }

        public NurseryItem(string pathName, string alias) {
            Ps = NewProcess(pathName);
            NurseryId = _idCursor++;
            Alias = alias;

            _launchLock = new object();

            Dogger.Debug("ProcessInfo created.");
        }

        /// <summary>
        /// Launch the process
        /// </summary>
        /// <returns></returns>
        public bool Launch() {
            Dogger.Trace($"Launch {Alias}");

            if (Switch is null) {
                Dogger.Error($"ToggleSwitch must set: {Alias}");
                return false;
            }

            // process is already running
            if (IsRunning) {
                Dogger.Warn($"Process {Alias} is running");
                return false;
            }

            // if this process had been redirected std-ioe, cancel
            if (RedirectIoe) {
                Ps.CancelOutputRead();
                Ps.CancelErrorRead();
                RedirectIoe = false;
            }

            lock (_launchLock) {
                IsRunning = true;
                bool launchSucceed = Ps.Start();

                if (!launchSucceed) { // launch failed
                    Dogger.Error($"Process launch failed: {Alias}");
                    return false;
                }
            }

            // TODO InvalidOperationExcepiton: process has exited.
            if (!Ps.HasExited) {
                CpuCounter = new PerformanceCounter("Process", "% Processor Time", Ps.ProcessName);
                MemCounter = new PerformanceCounter("Process", "Working Set - Private", Ps.ProcessName);
                Alias = Ps.ProcessName;
                // OnProcessLaunched?.Invoke(pd);
                Dogger.Info($"Process {Ps.ProcessName}[{Ps.Id}] launched successfully.");
            }

            // TODO System.InvalidOperationException:“An async read operation has already been started on the stream.”
            if (!RedirectIoe) {
                Ps.BeginOutputReadLine();
                Ps.BeginErrorReadLine();
                RedirectIoe = true;
            }

            return true;
        }

        /// <summary>
        /// Stop the process.
        /// </summary>
        public void Stop() {
            if (!Ps.HasExited) {
                Ps.Kill();
                Dogger.Info("Process killed.");
            } else {
                Dogger.Warn($"Process {Alias}({NurseryId}) already exited.");
            }
        }

        /// <summary>
        /// Stop the process and dispose the resource. 
        /// </summary>
        public void Dispose() {
            if (IsRunning) {
                Ps.Kill();
            }
            Ps.Dispose();
            Dogger.Trace($"Process disposed: {NurseryId}, {Alias}");
        }

        public override string ToString() => $"<ProcessInfo>{{Id:{NurseryId}, IsRunning:{IsRunning}, AutoRestart:{AutoRestart}, Alias:{Alias}}}";

        /// <summary>
        /// add a original process and init its start info or something else
        /// </summary>
        /// <param name="pathName">file's full name with path</param>
        private Process NewProcess(string pathName) {
            Dogger.Trace(pathName);
            Process child = new Process();
            child.StartInfo.CreateNoWindow = true;
            child.StartInfo.FileName = pathName;
            child.StartInfo.RedirectStandardError = true;
            child.StartInfo.RedirectStandardOutput = true;
            child.StartInfo.UseShellExecute = false;
            child.StartInfo.WorkingDirectory = Path.GetDirectoryName(pathName) ?? string.Empty;
            child.EnableRaisingEvents = true;

            child.OutputDataReceived += (s, e) => {
                if (!string.IsNullOrEmpty(e.Data)) Dogger.StdOutput(((Process)s).Id, e.Data);
            };

            child.ErrorDataReceived += (s, e) => {
                if (!string.IsNullOrEmpty(e.Data)) Dogger.StdError(((Process)s).Id, e.Data);
            };

            child.Exited += OnPsOnExited;

            return child;
        }

        private void OnPsOnExited(object sender, EventArgs _) {
            Dogger.Trace("Process exited." + Alias);

            if (AutoRestart && _restartCount < 3) {
                _restartCount++;
                Dogger.Info(Ps.Start() ? $"Restart {Ps.ProcessName}({NurseryId}) successfully." : $"Restart {Ps.ProcessName}({NurseryId}) failed.");
            } else {
                lock (_launchLock) { IsRunning = false; }
                OnProcessExited?.Invoke(this);
                Dogger.Info($"Process {Alias}, running:{IsRunning} exited.{(_restartCount > 0 ? $"(Restarted {_restartCount} times)" : "")}");
            }
        }
    }

}
