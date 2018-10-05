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
                    _BrowseInputCommand = new RelayCommand(x => InputFileName = BrowseInput(false).FirstOrDefault());
                return _BrowseInputCommand;
            }
        }

        protected override void DoRun()
        {
            RunConvert(InputFileName);
        }

        protected override bool DoCanRun()
        {
            return !string.IsNullOrEmpty(InputFileName);
        }
    }
}
