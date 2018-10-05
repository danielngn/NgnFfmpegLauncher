using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FfmpegLauncher.Models
{
    public class MergeTask : TaskBase
    {
        public ObservableCollection<string> FilesToMerge { get; } = new ObservableCollection<string>();

        private ICommand _AddFileCommand;
        public ICommand AddFileCommand
        {
            get
            {
                if (_AddFileCommand == null)
                    _AddFileCommand = new RelayCommand(x =>
                    {
                        var filenames = BrowseInput(true);
                        foreach (var filename in filenames)
                        {
                            if (!string.IsNullOrEmpty(filename))
                                FilesToMerge.Add(filename);
                        }
                    });
                return _AddFileCommand;
            }
        }

        private ICommand _RemoveFileCommand;
        public ICommand RemoveFileCommand
        {
            get
            {
                if (_RemoveFileCommand == null)
                    _RemoveFileCommand = new RelayCommand(x =>
                    {
                        if (!string.IsNullOrEmpty(SelectedFile))
                            FilesToMerge.Remove(SelectedFile);
                    }, x =>
                     {
                         return !string.IsNullOrEmpty(SelectedFile);
                     });
                return _RemoveFileCommand;
            }
        }

        public string SelectedFile { get; set; }

        protected override bool DoCanRun()
        {
            return FilesToMerge.Any();
        }

        protected override void DoRun()
        {
            bool hasError = false;
            string ext = null;
            foreach (var file in FilesToMerge)
            {
                var fi = new FileInfo(file);
                if (!fi.Exists)
                {
                    hasError = true;
                    LogError($"Can't find file {file}, abort.");
                }
                if (ext == null)
                    ext = fi.Extension;
                else
                {
                    if(!string.Equals(ext, fi.Extension, StringComparison.OrdinalIgnoreCase))
                    {
                        hasError = true;
                        LogError("Input files have different extentions, abort.");                        
                    }
                }
            }
            if (hasError)
                return;
            UiDispatcher.Invoke(new Action(() => Status = TaskStatus.Merging));
            var listFile = Path.GetTempFileName();
            var tempFileInfo = new FileInfo(Path.GetTempFileName());
            var concatFile = tempFileInfo.FullName.Replace(tempFileInfo.Extension, ext);
            File.WriteAllLines(listFile, FilesToMerge.Select(x => $"file \'{x}\'").ToArray());
            var mergeArg = $"-f concat -safe 0 -i \"{listFile}\" -c copy \"{concatFile}\"";
            LogInfo($"Task {TaskName} merge with arg:{mergeArg}");
            var exitCode = RunFfmpeg(mergeArg);
            if (exitCode != 0)
            {
                Status = TaskStatus.Failed;
                LogError($"Task {TaskName} merge failed ({exitCode}), abort.");
                return;
            }
            RunConvert(concatFile);
        }
    }
}
