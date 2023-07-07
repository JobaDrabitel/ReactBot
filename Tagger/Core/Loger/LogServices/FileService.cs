using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tagger.Core.Loger.Interfaces;

namespace Tagger.Core.Loger.LogServices
{
    internal class FileService<T> : ActionLoger, IFileLog
    {
        readonly object locker = new object();
        readonly string LogFilePath = "LogFile.txt";

        public async Task WriteLog(string message)
        {
            bool writeSucceeded = false;

            while (!writeSucceeded)
            {
                try
                {
                    lock (locker)
                    {
                        using (var writer = File.AppendText(LogFilePath))
                        {
                            writer.WriteLine(message);
                        }
                    }

                    writeSucceeded = true;
                }
                catch
                {
                    await Task.Delay(100);
                }
            }
        }

        public async Task<string> ReadLogs()
        {
            lock (locker)
            {
                return "";
            }
        }

        public override void Log(string message)
        {
            _ = WriteLog(message);
        }
    }
}
