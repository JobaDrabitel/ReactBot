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
                    if (TelegramClient.sendedMessages.Count > 0 && TelegramClient.taggedUsers.Count > 0)
                    {
                        users[i].SendedMessages = TelegramClient.sendedMessages[i];
                        users[i].TaggedUsers = TelegramClient.taggedUsers[i];

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

                    await Task.Delay(100);

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
            groups[g].TaggedUsers = TelegramClient.taggedUsersInGroup[g];

            if (TelegramClient.sendedMessagesInCycle.Count == groups.Count)
                groups[g].SendedMessagesInCurrentCycle = TelegramClient.sendedMessagesInCycle[g];

            groups[g].SendedMessages = TelegramClient.sendedMessagesInGroup[g];
        }

        private static async Task DataRefresh()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                window.StatisticListBox.Items.Refresh();
                window.ChatsListBox.Items.Refresh();

                window.AllMessagesLbl.Content = TelegramClient.messagesSended;
                window.CurrentGroupId.Content = TelegramClient.currentGroup;
                window.AllUsersLbl.Content = TelegramClient.taggedUsers.Sum();
                window.EndedGroups.Content = TelegramClient.endedChannels;
                window.AllPartLb.Content = TelegramClient.fullCount;
                window.UsersRestLb.Content = TelegramClient.count;
                window.EndedIteration.Content = TelegramClient.endedIteration;

                if (TelegramClient.messagesSended == 0)
                    window.AvgUserInMsg.Content = 0;
                else
                    window.AvgUserInMsg.Content = Math.Round((double)TelegramClient.taggedUsers.Sum() / TelegramClient.messagesSended, 2);
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
