using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Tagger.Core.Loger.Interfaces;

namespace Tagger.Core.Loger.LogServices
{
    internal class ListBoxService : ActionLoger, IFileLog
    {
        readonly object locker = new object();
        public static ListBox ListBox { get; set; }

        public async Task WriteLog(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {

                ListBox.Items.Add(message);
                ListBox.ScrollIntoView(ListBox.Items[ListBox.Items.Count - 1]);
            });
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
