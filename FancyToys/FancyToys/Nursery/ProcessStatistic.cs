using System.ComponentModel;

using NLog.LayoutRenderers;


namespace FancyToys.Nursery {

    public class ProcessStatistic: INotifyPropertyChanged {

        private const float GB = 1 << 30;

        private string process;
        private int pid;
        public float cpu;
        public float memory;
        private readonly int _nurseryId;

        public int GetNurseryId() => _nurseryId;

        public int PID {
            get => pid;
            set {
                pid = value;
                RaisePropertyChanged(nameof(PID));
            }
        }

        public string Process {
            get => process;
            set {
                process = value;
                RaisePropertyChanged(nameof(Process));
            }
        }

        public string CPU { get => $"{cpu:F}%"; }

        public string Memory { get => memory < GB ? $"{(int)memory >> 10:N0}KB" : $"{(int)memory >> 20:N0}MB"; }

        public void SetCPU(float _cpu) {
            cpu = _cpu;
            RaisePropertyChanged(nameof(CPU));
        }

        public void SetMemory(float mem) {
            memory = mem;
            RaisePropertyChanged(nameof(Memory));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ProcessStatistic(int nid, int psId, string psName, float cpu, float mem) {
            _nurseryId = nid;
            pid = psId;
            process = psName;
            this.cpu = cpu;
            memory = mem;
        }

        private void RaisePropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ProcessStatistic() { }

        public override string ToString() { return $"{{{Process}, {PID}, {CPU}, {Memory}}}"; }
    }

}
