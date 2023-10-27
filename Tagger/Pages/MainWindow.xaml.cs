using ChatBot.Core;
using Microsoft.Identity.Client;
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
        string _selectedStoryMediaPath;
        private DataModel _context = new DataModel();
        private LogService<MainWindow> _loger = new LogService<MainWindow>();
        static StreamWriter WTelegramLogs = new StreamWriter("WTelegram.log", true, Encoding.UTF8) { AutoFlush = true };
        public enum CustomEmojies : long
        { 
            GO = 5345822523474849927, 
            TO = 5343615391321044560, 
            LS = 5346216195882234448, 
            STORIS = 5343885261296121898, 
            STORIES = 5344003171033300686,
            INFO = 5346040626209108382,
			GOTOCHANNEL = 5260225573317262863, 
        }
 
        private ObservableCollection<UserData> users { get; set; } = new ObservableCollection<UserData>();

        public MainWindow()
        {
            InitializeComponent();
            WTelegram.Helpers.Log = (lvl, str) => WTelegramLogs.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{"TDIWE!"[lvl]}] {str}");
            WorkStatisticViewer.window = this;
			EmojiCB.ItemsSource = new List<string>
{
	"\U0001F609", // 😉
    "\U0001F604", // 😄
    "\U0001F60C", // 😌
    "\U0001F61A", // 😚
    "\U0001F61B", // 😛
    "\U0001F617", // 😗
    "\U0001F44D", // 👍
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
                foreach (var link in TelegramClient.inviteLinks)
                    TelegramClient.lastMessageInGroup.Add(0);
                await Task.Run(async () =>
                    Application.Current.Dispatcher.Invoke(async () =>
                        await client.Start(TelegramClient.inviteLinks[0], Convert.ToInt32(MessagesCountTB.Text), Convert.ToInt32(timeDelayTB.Text), emojiTB.Text, Convert.ToInt32(emojiCountTB.Text))));
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

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ListBoxService.ListBox = LogTextBox;
            MainInfoLoger.listBox = AccauntsTB;

            ProxiesListRefresh();
            BotsListRefresh();
        }

        private void BotsListRefresh()
        {
            var listItem = _context.Bots.Select(x => x.phone).ToList();
            BotsLB.ItemsSource = listItem;

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

        }

        private void RemoveProxyBtnClick(object sender, RoutedEventArgs e)
        {
            Proxies proxy = _context.Proxies.FirstOrDefault(x => x.ip == ProxyIPTB.Text);

            if (ProxyIPTB.Text != "")
            {
                _context.Proxies.Remove(proxy);
                _context.SaveChanges();


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

        }

        private void AddLink_Click(object sender, RoutedEventArgs e)
        {
            string[] links = GroupInviteLinkTB.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string link in links)
            {
                string formattedLink = FormatTelegramLink(link);

                if (!string.IsNullOrEmpty(formattedLink))
                {
                    if (!LinksLB.Items.Contains(formattedLink))
                    {
                        LinksLB.Items.Add(formattedLink);
                        TelegramClient.inviteLinks.Add(formattedLink);
                    }
                    else
                    {
                        MessageBox.Show("Такая ссылка уже добавлена");
                    }
                }
            }

            GroupInviteLinkTB.Clear();
        }

        private string FormatTelegramLink(string link)
        {
            if (link.StartsWith("@"))
            {
                string chatUsername = link.TrimStart('@');
                return "https://t.me/" + chatUsername;
            }

            return link;
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
        private void OpenStoryFileDialog_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Изображения (*.jpg, *.png)|*.jpg;*.png|Все файлы (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedStoryMediaPath = openFileDialog.FileName;
            }
        }

        private async void ChangeAccInfoButton_Click(object sender, RoutedEventArgs e)
        {
            TelegramClient telegramClient = new TelegramClient();
            var selectedItem = BotsLB.SelectedItem;
            if (selectedItem != null)
            {
                using (var dbContext = new DataModel())
                {
                    var matchingItem = dbContext.Bots.FirstOrDefault(bot => bot.phone == selectedItem);

                    if (matchingItem != null)
                    {
                        try
                        {
                            var selectedClient = await telegramClient.CreateClient(matchingItem);
                            await telegramClient.EditAccount(selectedClient, _selectedImagePath, UserFirstNameTB.Text, UserLastNameTB.Text, UserAboutTB.Text, UsernameTB.Text, _selectedStoryMediaPath, StoryCaptionTB.Text);
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

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            string allText = string.Join(Environment.NewLine, LinksLB.Items.OfType<string>());
            if (!string.IsNullOrEmpty(allText))
            {
                Clipboard.SetText(allText);
            }
        }

        private async void LeaveGroupButton_Click(object sender, RoutedEventArgs e)
        {
            MainInfoLoger.Log("Текущая группа пропускается");
            await TelegramClient.SkipGroup();
        }

		private void AddEmojiButton_Click(object sender, RoutedEventArgs e)
		{
            TelegramClient._emoji.Add(EmojiCB.SelectedItem.ToString());
			emojiCountTB.Text = (Convert.ToInt32(emojiCountTB.Text) + 1).ToString();
            EmojiesText.Text += EmojiCB.SelectedItem.ToString();
		}
	}
}
