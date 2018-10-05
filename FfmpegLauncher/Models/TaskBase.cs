using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Winform = System.Windows.Forms;

namespace FfmpegLauncher.Models
{
    public class TaskBase : NotifiableBase
    {
        public event EventHandler<LogEventArgs> AddLog;
        public string FfmpegFolder { get; set; }
        public Dispatcher UiDispatcher { get; set; }
        protected string FfmpegExec => FfmpegFolder + "\\ffmpeg.exe";
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

        public int BitRate { get; set; }

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
                OutputFileName = dialog.FileName;
        }

        protected string BrowseInput()
        {
            var dialog = new Winform.OpenFileDialog()
            {
                Title = "Select Input File Name",
                Filter = "All files (*.*)|*.*|MP4 files|*.mp4|MOV files|*.mov"
            };
            var result = dialog.ShowDialog();
            if (result == Winform.DialogResult.OK)
                return dialog.FileName;

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
                LogInfo($"Task {TaskName} completed in {duation} seconds.");
            }
            else
            {
                Status = TaskStatus.Failed;
                LogInfo($"Task {TaskName} failed.");
            }
            StatusMessage = $"{Environment.NewLine}{duation} seconds";
        }
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
