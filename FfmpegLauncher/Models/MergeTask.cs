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
                        var filename = BrowseInput();
                        if (!string.IsNullOrEmpty(filename))
                            FilesToMerge.Add(filename);
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
                    },x=>
                    {
                        return !string.IsNullOrEmpty(SelectedFile);
                    });
                return _RemoveFileCommand;
            }
        }

        public string SelectedFile { get; set; }
    }
}
