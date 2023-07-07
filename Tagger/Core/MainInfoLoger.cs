using System.Windows;
using System.Windows.Controls;

namespace Tagger.Core
{
    internal class MainInfoLoger
    {
        public static ListBox listBox;
        public static void Log(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                listBox.Items.Add(message);
                listBox.Items.Refresh();
            });
        }
    }
}
