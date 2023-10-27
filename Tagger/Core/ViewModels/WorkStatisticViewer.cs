using ChatBot.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Tagger.Core.ViewModels
{
    internal class WorkStatisticViewer
    {
        public static MainWindow window;
        private static List<UserData> users = new List<UserData>();
        private static List<GroupData> groups = new List<GroupData>();

        private static List<Stopwatch> stopwatches = new List<Stopwatch>();

        private static void StatisticRefresh()
        {
            Task.Run(async () =>
            {
                int i = 0;
                int g = 0;
                while (true)
                {
                    if (TelegramClient.sendedReactions.Count > 0)
                    {
                        users[i].SendedMessages = TelegramClient.sendedReactions[i];

                        GroupsDataRefresh(g);

                        if (users[i].FloodWait != 0)
                        {
                            if (users[i].FloodWait <= TelegramClient.isFloodWait[i] && users[i].FloodWait > 0)
                            {
                                TimeSpan time = stopwatches[i].Elapsed;
                                users[i].FloodWait = TelegramClient.isFloodWait[i] - (int)time.TotalSeconds;
                            }
                            else
                            {
                                stopwatches[i].Stop();
                                stopwatches[i].Reset();
                                users[i].FloodWait = 0;
                            }
                        }

                        if (TelegramClient.isFloodWait[i] != 0 && users[i].FloodWait == 0)
                        {
                            stopwatches[i].Start();
                            TimeSpan time = stopwatches[i].Elapsed;
                            users[i].FloodWait = TelegramClient.isFloodWait[i] - (int)time.TotalSeconds;
                        }
                    }

                    await DataRefresh();

                    await Task.Delay(1000);

                    if (i == users.Count - 1)
                        i = 0;
                    else
                        i++;

                    if (g == groups.Count - 1)
                        g = 0;
                    else
                        g++;
                }
            });
        }

        private static void GroupsDataRefresh(int g)
        {

            if (TelegramClient.sendedReactionsInCycle.Count == groups.Count)
                groups[g].SendedMessagesInCurrentCycle = TelegramClient.sendedReactionsInCycle[g];

            groups[g].SendedMessages = TelegramClient.sendedReactionsInGroup[g];
        }

        private static async Task DataRefresh()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                window.StatisticListBox.Items.Refresh();
                window.ChatsListBox.Items.Refresh();

                window.AllMessagesLbl.Content = TelegramClient.reactionsSended;
                window.CurrentGroupId.Content = TelegramClient.currentGroup;
                window.EndedGroups.Content = TelegramClient.endedChannels;
                window.EndedIteration.Content = TelegramClient.endedIteration;
            });
        }

        public static void StatisticLoad()
        {
            for (int i = 0; i < TelegramClient.bots.Count; i++)
            {
                users.Add(new UserData
                {
                    Phone = TelegramClient.bots[i].phone
                });

                stopwatches.Add(new Stopwatch());
            }

            for(int i = 0; i < TelegramClient.inviteLinks.Count; i++)
            {
                groups.Add(new GroupData
                {
                    Group = TelegramClient.inviteLinks[i]
                });
            }

            window.StatisticListBox.ItemsSource = users;
            window.ChatsListBox.ItemsSource = groups;

            StatisticRefresh();
        }
    }
}
