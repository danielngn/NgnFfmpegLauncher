using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FfmpegLauncher.Models
{
    public enum TaskStatus
    {
        NotRun,
        Validating,
        Merging,
        Converting,
        Succeeded,
        Failed
    }
}
