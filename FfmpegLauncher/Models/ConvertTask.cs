using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Winform = System.Windows.Forms;

namespace FfmpegLauncher.Models
{
    public class ConvertTask : TaskBase
    {
        private string _InputFileName;
        public string InputFileName
        {
            get { return _InputFileName; }
            set
            {
                _InputFileName = value;
                NotifyPropertyChanged(nameof(InputFileName));
            }
        }

        private ICommand _BrowseInputCommand;
        public ICommand BrowseInputCommand
        {
            get
            {
                if (_BrowseInputCommand == null)
                    _BrowseInputCommand = new RelayCommand(x => InputFileName = BrowseInput());
                return _BrowseInputCommand;
            }
        }

        protected override void DoRun()
        {
            UiDispatcher.Invoke(new Action(() => Status = TaskStatus.Converting));
            StringBuilder arg = new StringBuilder();
            if (UseHardwareDecode)
                arg.Append(" -hwaccel cuvid -c:v h264_cuvid ");
            arg.Append($"-i \"{InputFileName}\" ");
            if (UseHardwareEncode)
                arg.Append(" -vcodec h264_nvenc ");
            arg.Append($" -b:v {BitRate}M -maxrate:v {MaxBitRate}M ");
            arg.Append($"\"{OutputFileName}\"");
            LogInfo($"Task {TaskName} convert with arg:{arg.ToString()}");
            var psi = new ProcessStartInfo(FfmpegExec, arg.ToString()) { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true, UseShellExecute = false };
            var proc = Process.Start(psi);
            proc.WaitForExit(60*60*1000);
            Complete(proc.ExitCode);
        }

        protected override bool DoCanRun()
        {
            return !string.IsNullOrEmpty(InputFileName);
        }
    }
}
