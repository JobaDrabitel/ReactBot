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

        private bool isComplete = false;
        private bool isSynchro = false;

        public static int messagesSended = 0;
        public static int currentGroup = 0;
        public static int fullCount = 1;
        public static int count = 1;
        public static int endedChannels = 0;
        public static int endedIteration = 0;
        public static List<Task> tasks = new List<Task>();
        public static List<int> taggedUsers = new List<int>();
        public static List<int> sendedMessages = new List<int>();

        public static List<int> taggedUsersInGroup = new List<int>();
        public static List<int> sendedMessagesInGroup = new List<int>();
        public static List<int> sendedMessagesInCycle = new List<int>();
        public static IDictionary<long, User> Users = new Dictionary<long, User>();
        public static IDictionary<long, ChatBase> Chats = new Dictionary<long, ChatBase>();
        private bool isWork = false;

        public async Task Start(string inviteLink, string message, int timeDelay, int usersInMessage)
        {
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

                        taggedUsers.Add(0);
                        sendedMessages.Add(0);
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

            if (sendedMessagesInCycle.Count == 0)
                for (int i = 0; i < inviteLinks.Count; i++)
                    sendedMessagesInCycle.Add(0);

            if (!isWork)
            {
                for (int i = 0; i < inviteLinks.Count; i++)
                {
                    sendedMessagesInGroup.Add(0);
                    taggedUsersInGroup.Add(0);
                }

                WorkStatisticViewer.StatisticLoad();
                isWork = true;
            }

            await Task.Run(async () =>
            {
                count = 1;
                int i = 0;
                int offset = 0;
                while (count > 0 && isBanned.Contains(false))
                {
                    if (clientsStatus[i] && clients[i].User != null && !isBanned[i])
                    {
                        Client client = clients[i];
                        _loger.LogAction($"Начата рассылка: {client.User.username}");

                        await Task.Run(async () =>
                        {
                            _ = EditAccount(client, "a", "a", "a", "a", "avrgkhjfighfa");
                            _loger.LogAction($"Начата рассылка: {client.User.username}");
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

            isSynchro = false;
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
                    sendedMessagesInCycle.Clear();
                    endedIteration++;
                    currentGroup = 0;
                }
                else
                    currentGroup++;

                endedChannels++;
                await Start(inviteLinks[currentGroup], message, timeDelay, usersInMessage);
            }
        }

        private async Task<Client> CreateClient(Bots item)
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

        private async Task SpamStart(Client client, string message, int timeDelay, int offset, int usersInMessage)
        {
            Channels_ChannelParticipants channelParticipants = null;
            try
            {
                channelParticipants = await client.Channels_GetParticipants(inputPeers[clients.IndexOf(client)], new ChannelParticipantsMentions(), 200 * offset);
                _loger.LogAction($"Получено {channelParticipants.count} участников");
            }
            catch (Exception ex)
            {
                _loger.LogAction($"Не удалось подключится к каналу, возможно юзер {client.User.username} был забанен\nОшибка {ex}");
                MainInfoLoger.Log($"Не удалось подключится к каналу, возможно юзер {client.User.username} был забанен\nОшибка {ex}");
                clientsStatus[clients.IndexOf(client)] = false;
                isBanned[clients.IndexOf(client)] = true;
                IsForbidden(client, ex);

                return;
            }



            if (channelParticipants.participants.Count() == 0)
                count -= 200;

            if (!isSynchro)
            {
                fullCount = channelParticipants.count;
                count = fullCount;
                isSynchro = true;
            }

            List<User> id = new List<User>();
            foreach (var userID in channelParticipants.participants)
            {
                if (!userID.IsAdmin && !clients.Any(x => x.UserId == userID.UserId))
                {
                    channelParticipants.users.TryGetValue(userID.UserId, out var user);
                    id.Add(user);
                }
            }

            string tagMessage = "";
            int k = 0;
            Random random = new Random();
            Dictionary<long, User> users = new Dictionary<long, User>();

            for (int i = 0; i < id.Count; i++)
            {
                if (!users.ContainsValue(id[i]))
                {
                    users.Add(id[i].id, id[i]);
                    tagMessage += $"[ㅤ](tg://user?id={id[i].id}) ";
                    k++;
                }

                if ((k >= usersInMessage || i == id.Count - 1) && tagMessage != "")
                {
                    if (!isBanned[clients.IndexOf(client)])
                    {
                        MessageEntity[] messageEntities = new MessageEntity[usersInMessage];
                        messageEntities = Markdown.MarkdownToEntities(null, ref tagMessage, users: users);

                        int j = 0;
                        foreach (var mesEntity in messageEntities)
                            mesEntity.offset = message.Length + j++;

                        users.Clear();
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        Task longRunningTask = StartSend(client, message, inputPeers[clients.IndexOf(client)], tagMessage, messageEntities);
                        tagMessage = "";
                        Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                        Task completedTask = await Task.WhenAny(longRunningTask, timeoutTask);
                        stopwatch.Stop();

                        if (completedTask == longRunningTask)
                        {
                            _loger.LogAction($"Отправка сообщения закончена");
                            _loger.LogAction($"Время {stopwatch.Elapsed}");
                            _loger.LogAction($"Клиент: {client.User.phone}");

                            await Task.Delay(timeDelay + random.Next(100, 300));

                            taggedUsersInGroup[currentGroup] += k;
                            taggedUsers[clients.IndexOf(client)] += k;
                            count -= k;
                            _loger.LogAction($"Клиент: {client.User.phone} Тегнул юзеров: {taggedUsers[clients.IndexOf(client)]}");
                            k = 0;
                        }
                        else
                        {
                            _loger.LogAction($"Сообщение не отправилось за 5 секунд");
                            _loger.LogAction($"Клиент: {client.User.phone}");

                            tagMessage = "";
                            taggedUsers[clients.IndexOf(client)] += k;
                            taggedUsersInGroup[currentGroup] += k;
                            count -= k;
                            k = 0;

                            var floodWait = GetFloodWait();
                            _loger.LogAction($"Пользователь долго не отвечает. Заморозка потока на {floodWait} секунд");
                            MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{floodWait}");

                            isFloodWait[clients.IndexOf(client)] = floodWait;
                            await Task.Delay(floodWait * 1000);
                            isFloodWait[clients.IndexOf(client)] = 0;
                        }
                    }
                    else
                        return;
                }

            }

            clientsStatus[clients.IndexOf(client)] = true;
        }

        private async Task StartSend(Client client, string message, InputPeerChannel inputChat, string tagMessage, MessageEntity[] messageEntities)
        {
            try
            {
                _loger.LogAction($"Бот: {client.User.phone}\nОтправляем сообщение: {tagMessage}");
                var myMessage = await client.SendMessageAsync(inputChat, message + ' ' + tagMessage, entities: messageEntities);
                messagesSended++;
                sendedMessages[clients.IndexOf(client)]++;
                sendedMessagesInCycle[currentGroup]++;
                sendedMessagesInGroup[currentGroup]++;
            }
            catch (Exception rpcex)
            {
                if (rpcex.Message.Contains("FLOOD"))
                {
                    var floodWait = GetFloodWait();
                    _loger.LogAction($"Бот: {client.User.phone} FLOOD_WAIT_{floodWait}");
                    MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{floodWait}");

                    isFloodWait[clients.IndexOf(client)] = floodWait;
                    await Task.Delay(floodWait * 1000);
                    isFloodWait[clients.IndexOf(client)] = 0;

                    await StartSend(client, message, inputPeers[clients.IndexOf(client)], tagMessage, messageEntities);
                }

                IsForbidden(client, rpcex);
            }
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
            _loger.LogAction($"Вступает в чат: {client.UserId}");

            if (inputPeers[clients.IndexOf(client)] == null)
            {
                ChatBase chat = null;
                try
                {
                    if (inviteLink.Contains("+"))
                    {
                        Regex regex = new Regex(@"\+([A-Za-z0-9\-]+)");
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
                        catch (Exception ex)
                        {
                            chat = await client.AnalyzeInviteLink(inviteLink);
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
                    if (IsForbidden(client, ex))
                        return false;
                }
            }

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
                    return number + 2;
                }
            }

            return 0;
        }
        public async Task SendReactions(Client client)
        {
            var inputChannel = new InputChannel(inputPeers[0].channel_id, inputPeers[0].access_hash);
            var all_emoji = await client.Messages_GetAvailableReactions();
            var fullChannel = await client.Channels_GetFullChannel(inputChannel);
            Reaction reaction;
            if (fullChannel.full_chat.AvailableReactions is ChatReactionsSome some)
            {
                reaction = some.reactions[1];
            }
            else if (fullChannel.full_chat.AvailableReactions is ChatReactionsAll all)
            {
                if (all.flags.HasFlag(ChatReactionsAll.Flags.allow_custom) && client.User.flags.HasFlag(TL.User.Flags.premium))
                {
                    reaction = new ReactionCustomEmoji { document_id = 5190875290439525089 };
                }
                else
                {
                    reaction = new ReactionEmoji { emoticon = all_emoji.reactions[0].reaction };
                }
            }
            else
            {
                reaction = null;
            }

            if (reaction == null)
            {
                return;
            }
            for (int i = 0; i < 1000; i++)
            {
                var messages = await client.Messages_GetHistory(inputPeers[0], add_offset: i, limit: 1);
                foreach (var message in messages.Messages)
                {
                    if (i == 20)
                        _loger.LogAction($"Ставим реакцию под сообщением: {199}");
                    try
                    {
                        await client.Messages_SendReaction(inputPeers[0], message.ID, reaction: new[] { reaction });
                        _loger.LogAction($"Ставим реакцию под сообщением: {i}");
                        await Task.Delay(1000);
                    }
                    catch (Exception e) { _loger.LogAction(e.Message); }
                }
            }
        }
        private async Task EditAccount(Client client, string filePath, string firstName, string lastName, string about, string userName)
        {
            try
            {
                var inputFile = await client.UploadFileAsync(@"C:\Users\legen\Downloads\загружено.jpg");
                await client.Photos_UploadProfilePhoto(inputFile);
            }
            catch { _loger.LogAction($"Неверный путь до файла"); }
            await client.Account_UpdateProfile(firstName, lastName, about);
            try
            {
                await client.Account_UpdateUsername(userName);
            }
            catch(Exception ex ) { _loger.LogAction(ex.Message); }
            await Task.Delay(10000);
        }

    }
}
