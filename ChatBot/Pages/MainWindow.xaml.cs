using ChatBot.Core;
using ChatBot.Pages.DialogWindows;
using Starksoft.Net.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TL;
using WTelegram;

namespace ChatBot
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            TelegramClient.Start();
        }
    }
}
