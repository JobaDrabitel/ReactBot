using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tagger.Core.Loger.Interfaces
{
    internal interface IFileLog
    {
        Task WriteLog(string message);
        Task<string> ReadLogs();
    }
}
