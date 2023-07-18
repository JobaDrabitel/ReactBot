﻿using ChatBot.Core;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tagger.Core;
using Tagger.Core.Data;
using Tagger.Core.Loger.LogServices;
using Tagger.Core.ViewModels;

namespace Tagger
{

    public partial class MainWindow : Window
    {
        string _selectedImagePath;
        private DataModel _context = new DataModel();
        private LogService<MainWindow> _loger = new LogService<MainWindow>();
        static StreamWriter WTelegramLogs = new StreamWriter("WTelegram.log", true, Encoding.UTF8) { AutoFlush = true };


        private ObservableCollection<UserData> users { get; set; } = new ObservableCollection<UserData>();

        public MainWindow()
        {
            InitializeComponent();
            WTelegram.Helpers.Log = (lvl, str) => WTelegramLogs.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{"TDIWE!"[lvl]}] {str}");
            WorkStatisticViewer.window = this;
            EmojiCB.ItemsSource = new List<string>
            {
                "\U0001F389", // 🎉
                "\U0001F600", // 😀
                "\U0001F4A9", // 💩
                "\U0001F525", // 🔥
                "\U0001F44D", // 👍
                "\U0001F60D", // 😍
                "\U0001F496", // 💖
                "\U0001F4A1", // 💡
                "\U0001F60E", // 😎
                "\U0001F4AA"  // 💪
            };

        }

        private async void StartButtonClick(object sender, RoutedEventArgs e)
        {
            if (LinksLB.Items.Count != 0)
            {
                string selectedItem = EmojiCB.SelectedItem as string;
                _loger.LogAction("Начата работа...");
                if (selectedItem!= null)
                    emojiTB.Text = selectedItem;
                TelegramClient client = new TelegramClient();
                await Task.Run(async () =>
                    Application.Current.Dispatcher.Invoke(async () =>
                        await client.Start(TelegramClient.inviteLinks[0], Convert.ToInt32(MessagesCountTB.Text), Convert.ToInt32(timeDelayTB.Text), emojiTB.Text)
                    )) ;
            }
            else
                MessageBox.Show("Добавьте ссылки для начала работы");
        }

        private void AddBotButtonClick(object sender, RoutedEventArgs e)
        {
            if (ProxiesСB.SelectedItem != null)
            {
                var proxy = _context.Proxies.FirstOrDefault(x => x.ip == ProxiesСB.SelectedItem.ToString());

                if (!int.TryParse(APIIDTB.Text, out var apiId))
                    MessageBox.Show($"Неверное APIID");
                else
                {
                    Bots bot = new Bots()
                    {
                        phone = PhoneBotTB.Text,
                        password = PasswordBotTB.Text,
                        proxy_id = proxy.id,
                        Proxies = proxy,
                        api_hash = APIHASHTB.Text,
                        api_id = apiId,
                        Proxies1 = proxy,
                        Proxies2 = proxy
                    };

                    _context.Bots.AddOrUpdate(bot);
                    _context.SaveChanges();
                }

                _loger.LogAction($"Добавлен бот: {PhoneBotTB.Text}");

                BotsListRefresh();
            }
            else
            {
                MessageBox.Show("Выберите прокси");
            }
        }

        private void AddProxyButtonClick(object sender, RoutedEventArgs e)
        {
            Proxies proxies = new Proxies()
            {
                ip = ProxyIPTB.Text,
                port = int.Parse(ProxyPortTB.Text),
                login = ProxyLoginTB.Text,
                password = ProxyPasswordTB.Text,
                type = ProxyTypeCB.Text
            };

            _context.Proxies.AddOrUpdate(proxies);
            _context.SaveChanges();

            _loger.LogAction($"Добавлен прокси: {ProxyIPTB.Text}");

            ProxiesListRefresh();
        }

        private void ProxiesListRefresh()
        {
            IEnumerable list = null;

            try
            {
                list = _context.Proxies.Select(x => x.ip).ToList();
            }
            catch
            {

                var configuration = new Migrations.Configuration();
                var migrator = new DbMigrator(configuration);
                migrator.Update();
                var proxy = new Proxies
                {
                    ip = "0.0.0.0",
                    port = 0,
                    login = "0",
                    password = "0",
                    type = "LOCAL"
                };
                _context.Proxies.Add(proxy);
                list = _context.Proxies.Select(x => x.ip).ToList();
            }

            ProxiesСB.ItemsSource = list;
            ProxiesLV.ItemsSource = list;

            _loger.LogAction($"Списки прокси обновлены...");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ListBoxService.ListBox = LogTextBox;
            MainInfoLoger.listBox = AccauntsTB;
            _loger.LogAction($"Загрузка завершена");

            ProxiesListRefresh();
            BotsListRefresh();
        }

        private void BotsListRefresh()
        {
            var listItem = _context.Bots.Select(x => x.phone).ToList();
            BotsLB.ItemsSource = listItem;

            _loger.LogAction($"Списки ботов обновлены...");
        }

        private void RemoveBotButtonClick(object sender, RoutedEventArgs e)
        {
            Bots bot = _context.Bots.FirstOrDefault(x => x.phone == PhoneBotTB.Text);

            if (bot == null)
            {
                MessageBox.Show("Такого аккаунта нет");
                return;
            }

            _context.Bots.Remove(bot);
            _context.SaveChanges();

            _loger.LogAction($"Удалён бот: {PhoneBotTB.Text}");
            File.Delete(bot.phone);
            BotsListRefresh();
        }

        private void BotSelect(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            if (listBox.SelectedItem != null)
            {
                Bots bot = _context.Bots.FirstOrDefault(x => x.phone == listBox.SelectedItem.ToString());

                PhoneBotTB.Text = bot.phone;
                PasswordBotTB.Text = bot.password;
                if (bot.Proxies == null)
                    ProxiesСB.SelectedItem = "";
                else
                    ProxiesСB.SelectedItem = bot.Proxies.ip.ToString();
                APIHASHTB.Text = bot.api_hash;
                APIIDTB.Text = bot.api_id.ToString();
            }

            _loger.LogAction($"Выбран бот: {listBox.SelectedItem}");
        }

        private void RemoveProxyBtnClick(object sender, RoutedEventArgs e)
        {
            Proxies proxy = _context.Proxies.FirstOrDefault(x => x.ip == ProxyIPTB.Text);

            if (ProxyIPTB.Text != "")
            {
                _context.Proxies.Remove(proxy);
                _context.SaveChanges();

                _loger.LogAction($"Удалён прокси: {ProxyIPTB.Text}");

                ProxiesListRefresh();
            }
        }

        private void ProxySelected(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            if (listBox.SelectedItem != null)
            {
                Proxies proxy = _context.Proxies.FirstOrDefault(x => x.ip == listBox.SelectedItem.ToString());

                ProxyIPTB.Text = proxy.ip;
                ProxyPortTB.Text = proxy.port.ToString();
                ProxyLoginTB.Text = proxy.login;
                ProxyPasswordTB.Text = proxy.password;
                ProxyTypeCB.Text = proxy.type;
            }

            if (listBox.SelectedItem != null)
                _loger.LogAction($"Выбран прокси: {listBox.SelectedItem}");
        }

        private void AddLink_Click(object sender, RoutedEventArgs e)
        {
            string[] links = GroupInviteLinkTB.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string link in links)
            {
                if (!LinksLB.Items.Contains(link))
                {
                    LinksLB.Items.Add(link);
                    TelegramClient.inviteLinks.Add(link);
                }
                else
                {
                    MessageBox.Show("Такая ссылка уже добавлена");
                }
            }

            GroupInviteLinkTB.Clear();
        }
        private void LinksLB_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LinksLB.SelectedItem != null)
            {
                string selectedLink = LinksLB.SelectedItem.ToString();
                LinksLB.Items.Remove(selectedLink);
                TelegramClient.inviteLinks.Remove(selectedLink);
            }
        }
        private void OpenFileDialog_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Изображения (*.jpg, *.png)|*.jpg;*.png|Все файлы (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedImagePath = openFileDialog.FileName;
            }
        }

        private async void ChangeAccInfoButton_Click(object sender, RoutedEventArgs e)
        {
            TelegramClient telegramClient = new TelegramClient();
            var selectedItem = BotsLB.SelectedItem; // Получаем выбранный элемент из ListBox
            if (selectedItem != null)
            {
                using (var dbContext = new DataModel()) // Замените YourDbContext на ваш класс контекста EF
                {
                    // Выполняем LINQ-запрос для поиска первой записи с указанным номером телефона
                    var matchingItem = dbContext.Bots.FirstOrDefault(bot => bot.phone == selectedItem);

                    if (matchingItem != null)
                    {
                        try
                        {
                            var selectedClient = await telegramClient.CreateClient(matchingItem);
                            await telegramClient.EditAccount(selectedClient, _selectedImagePath, UserFirstNameTB.Text, UserLastNameTB.Text, UserAboutTB.Text, UsernameTB.Text);
                        }
                        catch (Exception ex) { _loger.LogAction(ex.Message); }
                        finally
                        {
                            MessageBox.Show("Данные успешно изменены!");
                        }
                    }
                }
            }
        }

    }
}
