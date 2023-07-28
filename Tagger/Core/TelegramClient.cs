using xNetStandard;
using System.Threading.Tasks;
using TL;
using WTelegram;
using System.Collections.Generic;
using System.Windows;
using Tagger.Pages;
using Tagger.Core.Data;
using System.Linq;
using Tagger.Core;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Tagger.Core.ViewModels;
using System.IO.Packaging;
using Microsoft.IdentityModel.Tokens;

namespace ChatBot.Core
{

    internal class TelegramClient
    {
        public static LogService<TelegramClient> _loger = new LogService<TelegramClient>();
        private static DataModel _context = new DataModel();
        public static List<string> logs = new List<string>();
        public static List<string> inviteLinks = new List<string>();
        public static List<Client> clients = new List<Client>();
        public static List<Bots> bots = new List<Bots>();
        public static List<bool> clientsStatus = new List<bool>();
        public static List<bool> isBanned = new List<bool>();
        public static List<int> isFloodWait = new List<int>();
        private List<InputPeerChannel> inputPeers = new List<InputPeerChannel>();
        static string _emoji;
        static Reaction _reaction;
        private bool isComplete = false;
        public static Messages_AvailableReactions all_emoji;
        public static int reactionsSended = 0;
        public static int currentGroup = 0;
        public static int fullCount = 1;
        public static int count = 1;
        public static int endedChannels = 0;
        public static int endedIteration = 0;
        public static List<Task> tasks = new List<Task>();
        public static List<int> sendedReactions = new List<int>();
        public static List<int> sendedReactionsInGroup = new List<int>();
        public static List<int> sendedReactionsInCycle = new List<int>();
        public static IDictionary<long, User> Users = new Dictionary<long, User>();
        public static IDictionary<long, ChatBase> Chats = new Dictionary<long, ChatBase>();
        private bool isWork = false;

        public async Task Start(string inviteLink, int countOfMessages, int timeDelay, string emoji)
        {
            _emoji = emoji;
            if (clients.Count == 0)
            {
                foreach (var item in _context.Bots)
                {
                    var client = await CreateClient(item);
                    if (client != null)
                    {
                        clients.Add(client);
                        clientsStatus.Add(true);
                        isFloodWait.Add(0);
                        sendedReactions.Add(0);
                    }
                }
            }

            for (int j = 0; j < clients.Count; j++)
            {
                tasks.Add(null);
                bots.Add(_context.Bots.ToList()[j]);
                isBanned.Add(false);
                inputPeers.Add(null);

                if (!await GetChannel(clients[j], inviteLink))
                {
                    clientsStatus[j] = false;
                    isBanned[j] = true;
                }
                else
                {
                    clientsStatus[j] = true;
                }
            }

            if (sendedReactionsInCycle.Count == 0)
                for (int i = 0; i < inviteLinks.Count; i++)
                    sendedReactionsInCycle.Add(0);

            if (!isWork)
            {
                for (int i = 0; i < inviteLinks.Count; i++)
                {
                    sendedReactionsInGroup.Add(0);
                }

                WorkStatisticViewer.StatisticLoad();
                isWork = true;
            }

            await Task.Run(async () =>
            {
                count = 1;
                int i = 0;
                int offset = 0;
                while (count <= countOfMessages && isBanned.Contains(false))
                {
                    if (clientsStatus[i] && clients[i].User != null && !isBanned[i])
                    {
                        Client client = clients[i];
                        _loger.LogAction($"Начата рассылка: {client.User.username} в {inviteLink}");

                        await Task.Run(async () =>
                        {
                            _ = SendReactions(client, countOfMessages, emoji, timeDelay, inviteLink);
                        });

                        clientsStatus[i] = false;
                        offset++;
                    }

                    if (i == clients.Count - 1)
                        i = 0;
                    else
                        i++;
                }
                count = 1;
            });

            foreach (var client in clients)
                if (inputPeers[clients.IndexOf(client)] != null)
                    try
                    {
                        await client.Channels_LeaveChannel(inputPeers[clients.IndexOf(client)]);
                    }
                    catch { }

            inputPeers.Clear();
            bots.Clear();
            isBanned.Clear();

            _loger.LogAction($"Отправка закончена в канал {inviteLink}");
            MainInfoLoger.Log($"Отправка закончена в канал {inviteLink}");
            await Task.Delay(timeDelay);

            if (!isComplete)
            {
                if (currentGroup == inviteLinks.Count - 1)
                {
                    sendedReactionsInCycle.Clear();
                    endedIteration++;
                    currentGroup = 0;
                }
                else
                    currentGroup++;

                endedChannels++;
                await Start(inviteLinks[currentGroup], countOfMessages, timeDelay, emoji);
            }
        }


        public async Task<Client> CreateClient(Bots item)
        {
            ProxyClient client = null;
            var proxies = _context.Proxies.FirstOrDefault(x => x.id == item.proxy_id);
            if (proxies.type.ToUpper() == "SOCKS5")
            {
                client = new Socks5ProxyClient()
                {
                    Host = proxies.ip,
                    Port = proxies.port,
                    Username = proxies.login,
                    Password = proxies.password
                };
            }
            else if (proxies.type.ToUpper() == "HTTPS")
            {
                client = new HttpProxyClient()
                {
                    Host = proxies.ip,
                    Port = proxies.port,
                    Username = proxies.login,
                    Password = proxies.password
                };
            }
            _loger.LogAction($"Получение пользователя: {item.phone}");
            Client thisClient = GetUserProxy(item.phone, client, item);
            if (thisClient != null)
            {
                _loger.LogAction($"Авторизация пользователя: {item.phone}");
                try
                {
                    if (thisClient.User == null)
                    {
                        await thisClient.LoginUserIfNeeded();
                        await Task.Delay(2000);
                    }
                }
                catch (Exception ex)
                {
                    MainInfoLoger.Log($"Аккаунт {item.phone} забанен или данные введены неверно, попробуйте снова.\nОшибка: {ex.Message}\nПодробнее: {ex.InnerException}");
                    return null;
                }
            }
            return thisClient;
        }

        private Client GetUserProxy(string sessionPathname, ProxyClient proxy, Bots bot)
        {
            try
            {
                var client = new Client(what => GetConfig(what, bot));
                _loger.LogAction($"Подключение пользователя к прокси: {sessionPathname}");
                if (proxy == null)
                    return client;
                client.TcpHandler = async (address, port) =>
                {
                    return proxy.CreateConnection(address, port);
                };
                return client;
            }
            catch
            {
                MessageBox.Show("Не удается получить конфиг. Попрбуйте перезагрузить приложение и попробуйте снова.");
            }
            return null;
        }

        private static bool IsForbidden(Client client, Exception rpcex)
        {
            if (rpcex.Message.ToUpper().Contains("FORBIDDEN") || rpcex.Message.ToUpper().Contains("PRIVATE"))
            {
                isBanned[clients.IndexOf(client)] = true;
                _loger.LogAction(rpcex.Message);
                return true;
            }
            else if (rpcex.Message.ToUpper().Contains("BAN"))
            {
                isBanned[clients.IndexOf(client)] = true;
                _loger.LogAction(rpcex.Message);
                MainInfoLoger.Log($"{client.User.phone} забанен или находится в спам-блоке");
                return true;
            }

            _loger.LogAction(rpcex.Message);
            isBanned[clients.IndexOf(client)] = true;

            return false;
        }

        private async Task<bool> GetChannel(Client client, string inviteLink)
        {
            string[] groupName = inviteLink.Split('/');
            _loger.LogAction($"Вступает в чат: {client.UserId}");

            if (inputPeers[clients.IndexOf(client)] == null)
            {
                ChatBase chat = null;
                try
                {
                    if (inviteLink.Contains("+"))
                    {
                        Regex regex = new Regex(@"\+([A-Za-z0-9\-_]+)");
                        Match match = regex.Match(inviteLink);
                        if (match.Success)
                        {
                            string hash = match.Groups[1].Value;
                            await client.Messages_ImportChatInvite(hash);
                            var chatInvite = (ChatInviteAlready)await client.Messages_CheckChatInvite(hash);
                            chat = chatInvite.chat;
                        }
                    }
                    else
                        try
                        {
                            try
                            {
                                chat = await client.AnalyzeInviteLink(inviteLink, true);
                            }
                           
                            catch (RpcException ex)
                            {
                                if (ex.Message.Contains("FLOOD"))
                                {
                                    var floodWait = GetFloodWait();
                                    _loger.LogAction($"Бот: {client.User.phone} FLOOD_WAIT_{floodWait}");
                                    MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{floodWait}");
                                    isFloodWait[clients.IndexOf(client)] = floodWait;
                                    await Task.Delay(floodWait * 1000);
                                    isFloodWait[clients.IndexOf(client)] = 0;
                                }
                                if (IsForbidden(client, ex))
                                    return false;

                                return false;
                            }
                            catch (Exception exc)
                            {
                                chat = await client.AnalyzeInviteLink(inviteLink);
                                _loger.LogAction($"{exc}");
                            }
                        }
                        catch (RpcException ex)
                        {
                            if (ex.Message.Contains("FLOOD"))
                            {
                                var floodWait = GetFloodWait();
                                _loger.LogAction($"Бот: {client.User.phone} FLOOD_WAIT_{floodWait}");
                                MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{floodWait}");
                                isFloodWait[clients.IndexOf(client)] = floodWait;
                                await Task.Delay(floodWait * 1000);
                                isFloodWait[clients.IndexOf(client)] = 0;
                            }
                            if (IsForbidden(client, ex))
                                return false;

                            return false;
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                chat = await client.AnalyzeInviteLink(inviteLink, true);
                            }
                            catch (Exception exc)
                            {
                                chat = await client.AnalyzeInviteLink(inviteLink);
                                _loger.LogAction($"{exc}");
                            }
                            if (IsForbidden(client, ex))
                                return false;
                        }


                    var inputChannel = new InputPeerChannel(chat.ID, ((Channel)chat).access_hash);
                    inputPeers[clients.IndexOf(client)] = inputChannel;

                    await client.Folders_EditPeerFolders(new InputFolderPeer
                    {
                        folder_id = 1,
                        peer = inputChannel
                    });
                }
                catch (Exception ex)
                {
                    IsForbidden(client, ex);
                    return false;
                }
            }
            if (inputPeers[clients.IndexOf(client)] == null)
                return false;
            _loger.LogAction($"Получаем чат...");
            return true;
        }

        static string GetConfig(string what, Bots client)
        {
            _loger.LogAction($"Получаем конфиг: {what}");
            switch (what)
            {
                case "session_key": return client.api_hash;
                case "api_id": return client.api_id.Value.ToString();
                case "api_hash": return client.api_hash;
                case "phone_number": return client.phone;
                case "verification_code":
                    string code = null;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        VerificationCodeDialogPage verificationCode = new VerificationCodeDialogPage(client.phone);
                        verificationCode.ShowDialog();
                        code = verificationCode.Code;
                    });
                    return code;
                case "first_name": return "John";
                case "password": return client.password;
                case "session_pathname": return "sessions/" + client.phone;
                default:
                    return null;
            }
        }
        static int GetFloodWait()
        {
            Helpers.Log += (lvl, str) => logs.Add(str);
            string pattern = @"FLOOD_WAIT_(\d+)";

            for (int i = logs.Count - 1; i >= 0; i--)
            {
                var log = logs[i];
                Match match = Regex.Match(log, pattern);

                if (match.Success)
                {
                    string numberString = match.Groups[1].Value;
                    int.TryParse(numberString, out int number);
                    logs.Clear();
                    return number;
                }
            }

            return 0;
        }
        public async Task SendReactions(Client client, int countOfMessages, string reactionEmoji, int delay, string inviteLink)
        {
            List<long> reactorsId = new List<long>();
            _reaction = await CheckReactions(client, reactionEmoji);

            if (_reaction == null)
            {
                isBanned[clients.IndexOf(client)] = true;
                clientsStatus[clients.IndexOf(client)] = true;
                return;
            }
            for (int i = 0; i < countOfMessages; i += 100)
            {
                try
                {
                    var messages = await client.Messages_GetHistory(inputPeers[clients.IndexOf(client)], add_offset: i, limit: 100);
                    if (messages.Messages.Count() == 0)
                    {
                        clientsStatus[clients.IndexOf(client)] = true;
                        isBanned[clients.IndexOf(client)] = true;
                        return;
                    }
                    foreach (var messageBase in messages.Messages)
                    {
                        if (messageBase is MessageService messageService)
                            continue;
                        if (messageBase is Message message)
                            if (message.reactions != null && message.reactions.recent_reactions != null)
                                foreach (var reactors in message.reactions.recent_reactions)
                                    reactorsId.Add(reactors.peer_id.ID);
                        if (reactorsId.Contains(client.User.id) == false)
                        {

                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();
                            Task longRunningTask = SendReactionToMessageAsync(client, delay, messageBase, countOfMessages);
                            if (_reaction == null)
                            {
                                isBanned[clients.IndexOf(client)] = true;
                                clientsStatus[clients.IndexOf(client)] = true;
                                return;
                            }
                            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                            Task completedTask = await Task.WhenAny(longRunningTask, timeoutTask);
                            stopwatch.Stop();

                            if (completedTask == longRunningTask)
                            {
                                _loger.LogAction($"Отправка сообщения закончена");
                                _loger.LogAction($"Время {stopwatch.Elapsed}");
                                _loger.LogAction($"Клиент: {client.User.phone}");
                                reactionsSended++;
                                sendedReactions[clients.IndexOf(client)]++;
                                sendedReactionsInCycle[inviteLinks.IndexOf(inviteLink)]++;
                                sendedReactionsInGroup[inviteLinks.IndexOf(inviteLink)]++;
                                count++;
                                await Task.Delay(delay);
                            }
                            else
                            {
                                _loger.LogAction($"Реакция не отправилась за 5 секунд");
                                _loger.LogAction($"Клиент: {client.User.phone}");
                                var floodWait = GetFloodWait();
                                if (floodWait != 0)
                                {
                                    _loger.LogAction($"Пользователь долго не отвечает. Флудвейт на {floodWait} секунд");
                                    MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{floodWait}");
                                    isFloodWait[clients.IndexOf(client)] = floodWait;
                                    await Task.Delay(floodWait * 1000);
                                    isFloodWait[clients.IndexOf(client)] = 0;
                                }
                            }
                        }
                        reactorsId.Clear();
                    }
                }
                catch { }
            }
            clientsStatus[clients.IndexOf(client)] = true;
            isBanned[clients.IndexOf(client)] = true;
        }

        private async Task<Reaction> CheckReactions(Client client, string reactionEmoji)
        {
            Random random = new Random();
            InputChannel inputChannel = null;
            try
            {
                inputChannel = new InputChannel(inputPeers[clients.IndexOf(client)].channel_id, inputPeers[clients.IndexOf(client)].access_hash);
            }
            catch
            {
                MainInfoLoger.Log($"Не удалось получить группу, возможно юзер {client.User.phone} забанен");
                return null;
            }
            if (all_emoji == null)
            {
                try
                {
                    all_emoji = await client.Messages_GetAvailableReactions();
                }
                catch
                {
                    all_emoji = null;
                }
            }
            try
            {
                var fullChannel = await client.Channels_GetFullChannel(inputChannel);
                if (fullChannel.full_chat.AvailableReactions is ChatReactionsSome some)
                {
                    _reaction = some.reactions[1];
                }
                else if (fullChannel.full_chat.AvailableReactions is ChatReactionsAll all)
                {
                    if (reactionEmoji.IsNullOrEmpty())
                        reactionEmoji = all_emoji.reactions[random.Next(all_emoji.reactions.Length - 1)].reaction;
                    if (all.flags.HasFlag(ChatReactionsAll.Flags.allow_custom) && client.User.flags.HasFlag(TL.User.Flags.premium))
                    {
                        var reactList = await client.Messages_SearchCustomEmoji(reactionEmoji);
                        _reaction = new ReactionCustomEmoji { document_id = reactList.document_id[random.Next(0, reactList.document_id.Length-1)] };
                    }
                    else
                    {
                        try
                        {
                            _reaction = new ReactionEmoji
                            {
                                emoticon = all_emoji.reactions.FirstOrDefault(r => r.reaction == reactionEmoji).reaction.ToString()
                            };
                        }
                        catch
                        {
                            _loger.LogAction($"Эмодзи {reactionEmoji} не поддерживается в текущей группе, будет выбрано одно из доступных");
                            MainInfoLoger.Log($"Эмодзи {reactionEmoji} не поддерживается в текущей группе, будет выбрано одно из доступных");
                            _reaction = new ReactionEmoji
                            {
                                emoticon = all_emoji.reactions[0].reaction
                            };
                        }
                    }
                }
                else
                {
                    _reaction = null;
                }

                return _reaction;
            }
            catch { return null; }
        }

        private async Task SendReactionToMessageAsync(Client client, int delay, MessageBase message, int countOfMessages)
        {
            try
            {
                await client.Messages_SendReaction(inputPeers[clients.IndexOf(client)], message.ID, reaction: new[] { _reaction });
                _loger.LogAction($"Ставим реакцию {reactionsSended}");
            }
            catch (Exception e)
            {
                if (e.Message.Contains("FLOOD"))
                {
                    var floodWait = GetFloodWait();
                    if (floodWait != 0)
                    {
                        _loger.LogAction($"Бот: {client.User.phone} FLOOD_WAIT_{floodWait}");
                        MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{floodWait}");
                        isFloodWait[clients.IndexOf(client)] = floodWait;
                        await Task.Delay(floodWait * 1000);
                        isFloodWait[clients.IndexOf(client)] = 0;
                        await SendReactionToMessageAsync(client, delay, message, countOfMessages);
                    }
                }
                if (e.Message.Contains("FORBIDDEN"))
                {
                    IsForbidden(client, e);
                    return;
                }
                else
                {
                    MainInfoLoger.Log($"Реакция недоступна, проверка на доступные реакции");
                    _reaction = await CheckReactions(client, _emoji);
                    if (_reaction == null)
                    {
                        MainInfoLoger.Log($"В группе запрещены реакции");
                        return;
                    }
                    await SendReactionToMessageAsync(client, delay, message, countOfMessages);
                }

                _loger.LogAction(e.Message);
            }
        }

        public async Task EditAccount(Client client, string filePath, string firstName, string lastName, string about, string userName)
        {

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    var inputFile = await client.UploadFileAsync(filePath);
                    await client.Photos_UploadProfilePhoto(inputFile);
                }
                catch
                {
                    _loger.LogAction($"Неверный путь до файла");
                    MessageBox.Show("Возникла ошибка изменения фотографии, возможно, путь указан неверно");
                }
            }

            firstName = firstName == "" ? null : firstName;
            lastName = lastName == "" ? null : lastName;
            about = about == "" ? null : about;

            try
            {

                await client.Account_UpdateProfile(firstName, lastName, about);
            }
            catch
            {
                MessageBox.Show("Возникла ошибка изменения имени, фамилии или статуса, возможно были использованы некорректные символы");
            }
            try
            {
                await client.Account_UpdateUsername(userName);
            }
            catch (Exception ex)
            {
                _loger.LogAction(ex.Message);
                MessageBox.Show("Возникла ошибка изменения юзернейма, возможно, ваш вариант занят или были использованы некорректные символы");
            }
            await Task.Delay(1000);
        }
    }
}
