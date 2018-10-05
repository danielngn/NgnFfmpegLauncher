using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FfmpegLauncher.Models
{
    public enum LogCategory
    {
        Info,
        Error
    }

    public class LogItem
    {
        public LogCategory Category { get; set; }
        public string Message { get; set; }
    }
}
