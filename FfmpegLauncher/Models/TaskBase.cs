using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Winform = System.Windows.Forms;

namespace FfmpegLauncher.Models
{
    public class TaskBase : NotifiableBase
    {
        public TaskBase()
        {

        }

        public TaskBase(ObservableCollection<string> outputFolders)
        {
            OutputFolders = outputFolders;
        }

        public static string FfmpegExec { get; set; }
        public event EventHandler<LogEventArgs> AddLog;
        public Dispatcher UiDispatcher { get; set; }
        protected string TaskName { get; private set; }
        protected DateTime StartTime { get; private set; }
        protected DateTime EndTime { get; private set; }

        public bool UseHardwareEncode { get; set; }
        public bool UseHardwareDecode { get; set; }

        private string _outputFileName;
        public string OutputFileName
        {
            get { return _outputFileName; }
            set
            {
                _outputFileName = value;
                NotifyPropertyChanged(nameof(OutputFileName));
            }
        }

        private int _bitRate;
        public int BitRate {
            get => _bitRate;
            set
            {
                _bitRate = value;
                NotifyPropertyChanged(nameof(BitRate));
            }
        }

        private int _maxBitRate;
        public int MaxBitRate
        {
            get { return _maxBitRate; }
            set
            {
                _maxBitRate = value;
                NotifyPropertyChanged(nameof(MaxBitRate));
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                _statusMessage = value;
                NotifyPropertyChanged(nameof(StatusMessage));
            }
        }

        private bool _isReadOnly;
        public bool IsEditable
        {
            get { return _isReadOnly; }
            set
            {
                _isReadOnly = value;
                NotifyPropertyChanged(nameof(IsEditable));
            }
        }

        private TaskStatus _status;
        public TaskStatus Status
        {
            get { return _status; }
            set
            {
                if (value == _status)
                    return;
                _status = value;
                NotifyPropertyChanged(nameof(Status));

            }
        }

        private ICommand _RunCommand;
        public ICommand RunCommand
        {
            get
            {
                if (_RunCommand == null)
                    _RunCommand = new RelayCommand(x => RunTask(), x => CanRunTask());
                return _RunCommand;
            }
        }

        protected void SetStatus(TaskStatus status)
        {
            UiDispatcher.Invoke(new Action(() => Status = status));
        }

        public async Task RunTask()
        {
            StartTime = DateTime.Now;
            SetStatus(TaskStatus.Validating);
            await Task.Factory.StartNew(() =>
            {
                if (!File.Exists(FfmpegExec))
                {
                    LogError($"Can't file ffmpeg executable: {FfmpegExec}, abort.");
                    return;
                }
                if (BitRate <= 0)
                {
                    LogError($"Invalid bitrate: {BitRate}, abort.");
                    return;
                }
                if (MaxBitRate < BitRate)
                {
                    MaxBitRate = BitRate;
                }
                var fi = new FileInfo(OutputFileName);
                TaskName = fi.Name;
                LogInfo($"Task {TaskName} starting...");
                try
                {
                    if (fi.Exists)
                    {
                        LogInfo($"Output file {OutputFileName} already exist, deleting...");
                        fi.Delete();
                        LogInfo($"Deleted file {OutputFileName}.");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to delete file {OutputFileName}, abort task. {ex.ToString()}");
                    return;
                }
                DoRun();
            });
        }

        private bool CanRunTask()
        {
            return Status != TaskStatus.Converting && Status != TaskStatus.Merging
                && !string.IsNullOrEmpty(OutputFileName) && DoCanRun();
        }

        protected virtual bool DoCanRun()
        {
            return true;
        }

        protected virtual void DoRun()
        {

        }

        private ICommand _BrowseOutputCommand;
        public ICommand BrowseOutputCommand
        {
            get
            {
                if (_BrowseOutputCommand == null)
                    _BrowseOutputCommand = new RelayCommand(x => BrowseOutput());
                return _BrowseOutputCommand;
            }
        }

        private void BrowseOutput()
        {
            var dialog = new Winform.SaveFileDialog()
            {
                DefaultExt = "mp4",
                Title = "Select Output File Name",
                Filter = "MP4 files|*.mp4|All files (*.*)|*.*"
            };
            var result = dialog.ShowDialog();
            if (result == Winform.DialogResult.OK)
            {
                OutputFileName = dialog.FileName;
                var file = new FileInfo(OutputFileName);
                var dir = file.DirectoryName;
                var existing = OutputFolders.FirstOrDefault(x => x.Equals(dir, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    OutputFolders.Insert(0, dir);
                }
                else
                {
                    OutputFolders.Move(OutputFolders.IndexOf(existing), 0);
                }
            }
        }

        protected string[] BrowseInput(bool isMultiFile)
        {
            var dialog = new Winform.OpenFileDialog()
            {
                Title = "Select Input File Name",
                Filter = "All files (*.*)|*.*|MP4 files|*.mp4|MOV files|*.mov"
            };
            if (isMultiFile)
            {
                dialog.Multiselect = true;
            }
            var result = dialog.ShowDialog();
            if (result == Winform.DialogResult.OK)
                return dialog.FileNames;

            return null;
        }

        protected void LogInfo(string message)
        {
            AddLog(this, new LogEventArgs(message, LogCategory.Info));
        }

        protected void LogError(string message)
        {
            AddLog(this, new LogEventArgs(message, LogCategory.Error));
        }

        protected void Complete(int exitCode)
        {
            EndTime = DateTime.Now;
            var duation = (EndTime - StartTime).TotalSeconds.ToString("0.");
            if (exitCode == 0)
            {
                Status = TaskStatus.Succeeded;
                LogInfo($"Task {TaskName} convert succeeded in {duation} seconds.");
            }
            else
            {
                Status = TaskStatus.Failed;
                LogError($"Task {TaskName} convert failed.");
            }
            StatusMessage = $"{Environment.NewLine}{duation} seconds";
        }

        protected void RunConvert(string inputFileName)
        {
            if (!File.Exists(inputFileName))
            {
                LogError($"Can't find file {inputFileName}, abort.");
                return;
            }
            UiDispatcher.Invoke(new Action(() => Status = TaskStatus.Converting));
            StringBuilder arg = new StringBuilder();
            if (UseHardwareDecode)
                arg.Append(" -hwaccel cuvid -c:v h264_cuvid ");
            arg.Append($"-i \"{inputFileName}\" ");
            if (UseHardwareEncode)
                arg.Append(" -vcodec h264_nvenc ");
            arg.Append($" -b:v {BitRate}M -maxrate:v {MaxBitRate}M ");
            arg.Append($"\"{OutputFileName}\"");
            LogInfo($"Task {TaskName} convert with arg:{arg.ToString()}");
            Complete(RunFfmpeg(arg.ToString()));
        }

        protected int RunFfmpeg(string arg)
        {
            var psi = new ProcessStartInfo(FfmpegExec, arg) { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true, UseShellExecute = false };
            var proc = Process.Start(psi);
            proc.WaitForExit(60 * 60 * 1000);
            return proc.ExitCode;
        }

        public ObservableCollection<string> OutputFolders { get; }
    }

    public class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public LogCategory Category { get; }

        public LogEventArgs(string message, LogCategory category)
        {
            Message = message;
            Category = category;
        }
    }
}
