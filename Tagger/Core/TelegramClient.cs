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
using static Tagger.MainWindow;

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
        public static List<string> _emoji = new List<string>();
        static int currentCicle = 0;
        static List<Reaction> _reaction;
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
        public static List<int> lastMessageInGroup = new List<int>();
        public static List<int> sendedReactionsInGroup = new List<int>();
        public static List<int> sendedReactionsInCycle = new List<int>();
        public static IDictionary<long, User> Users = new Dictionary<long, User>();
        public static IDictionary<long, ChatBase> Chats = new Dictionary<long, ChatBase>();
        private bool isWork = false;
        private int _emojiCount;
        public async Task Start(string inviteLink, int countOfMessages, int timeDelay, string emoji, int emojiCount)
        {
            while (true)
            {
                if (!isComplete)
                {
                    await StartGroup(inviteLinks[currentGroup], countOfMessages, timeDelay, emoji, emojiCount);

                    if (currentGroup == inviteLinks.Count - 1)
                    {
                        sendedReactionsInCycle.Clear();
                        endedIteration++;
                        currentGroup = 0;
                    }
                    else
                        currentGroup++;

                    endedChannels++;
                }
                else
                    break;
            }
        }
        public async Task StartGroup(string inviteLink, int countOfMessages, int timeDelay, string emoji, int emojiCount)
        {
            if (_reaction == null)
                _reaction = new List<Reaction>();
            _emojiCount = emojiCount;
            if(_emoji.Count == 0)
                _emoji = emoji.Split(' ').ToList();
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

                        await Task.Run(async () =>
                        {
                            _ = SendReactions(client, countOfMessages, _emoji, timeDelay, inviteLink, emojiCount);
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

            MainInfoLoger.Log($"Отправка закончена в канал {inviteLink}");
            await Task.Delay(timeDelay);


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
            Client thisClient = GetUserProxy(item.phone, client, item);
            if (thisClient != null)
            {
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
                MainInfoLoger.Log($"{client.User.phone} забанен или находится в спам-блоке");
                return true;
            }
            else if (rpcex.Message.ToUpper().Contains("BAN"))
            {
                isBanned[clients.IndexOf(client)] = true;
                MainInfoLoger.Log($"{client.User.phone} забанен или находится в спам-блоке");
                return true;
            }

            isBanned[clients.IndexOf(client)] = true;

            return false;
        }

        private async Task<bool> GetChannel(Client client, string inviteLink)
        {
            string[] groupName = inviteLink.Split('/');

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
                            try
                            {
                                await client.Messages_ImportChatInvite(hash);
                                var chatInvite = (ChatInviteAlready)await client.Messages_CheckChatInvite(hash);
                                chat = chatInvite.chat;
                            }
                            catch (RpcException rpcex)
                            {
                                if (rpcex.Message.ToUpper().Contains("ALREADY") || rpcex.Message.ToUpper().Contains("PARTIPICANT"))
                                {
                                    var chatInvite = (ChatInviteAlready)await client.Messages_CheckChatInvite(hash);
                                    chat = chatInvite.chat;
                                }
                                else
                                {
                                    IsForbidden(client, rpcex);
                                    return false;
                                }
                            }
                        }
                    }
                    else
                        try
                        {
                            var test = await client.Contacts_Search(inviteLink);
                            chat = test.chats.Values.ToList().FirstOrDefault(x => x.MainUsername == groupName.Last());
                            try
                            {
                                await client.Channels_JoinChannel((Channel)chat);
                            }
                            catch (NullReferenceException nullex)
                            {
                                try
                                {
                                    chat = await client.AnalyzeInviteLink(inviteLink, true);
                                }
                                catch (Exception ex)
                                {
                                    if (ex.Message.Contains("FLOOD"))
                                    {
                                        var floodWait = GetFloodWait();
                                        if (floodWait > 0)
                                        {
                                            MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{floodWait}");
                                            isFloodWait[clients.IndexOf(client)] = floodWait;
                                            await Task.Delay(floodWait * 1000);
                                            isFloodWait[clients.IndexOf(client)] = 0;
                                        }
                                        else
                                        {
                                            MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{100}");
                                            await Task.Delay(100 * 1000);
                                        }
                                    }
                                    if (IsForbidden(client, ex))
                                        return false;
                                    chat = await client.AnalyzeInviteLink(inviteLink);
                                }
                            }
                            catch (Exception exc)
                            {
                                if (exc.Message.Contains("FLOOD"))
                                {
                                    var floodWait = GetFloodWait();
                                    if (floodWait > 0)
                                    {
                                        MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{floodWait}");
                                        isFloodWait[clients.IndexOf(client)] = floodWait;
                                        await Task.Delay(floodWait * 1000);
                                        isFloodWait[clients.IndexOf(client)] = 0;
                                    }
                                    else
                                    {
                                        MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{100}");
                                        await Task.Delay(100 * 1000);
                                    }
                                }
                                if (IsForbidden(client, exc))
                                    return false;
                                chat = await client.AnalyzeInviteLink(inviteLink);

                            }
                        }
                        catch (RpcException ex)
                        {
                            if (ex.Message.Contains("FLOOD"))
                            {
                                var floodWait = GetFloodWait();
                                if (floodWait > 0)
                                {
                                    MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{floodWait}");
                                    isFloodWait[clients.IndexOf(client)] = floodWait;
                                    await Task.Delay(floodWait * 1000);
                                    isFloodWait[clients.IndexOf(client)] = 0;
                                }
                                else
                                {
                                    MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{100}");
                                    await Task.Delay(100 * 1000);
                                }
                            }
                            if (IsForbidden(client, ex))
                                return false;

                            return false;
                        }
                        catch (Exception ex)
                        {
                            var test = await client.Contacts_Search(inviteLink);
                            chat = test.chats.Values.ToList().FirstOrDefault(x => x.MainUsername == groupName.Last());
                            var c = (Channel)chat;
                            var cf = await client.Channels_GetFullChannel((InputChannel)c);
                            var chanfull = (ChannelFull)cf.full_chat;
                            var chats = new InputPeerChat(chanfull.linked_chat_id);
                            try
                            {
                                await client.Channels_JoinChannel((Channel)chat);
                            }
                            catch (NullReferenceException nullex)
                            {
                                try
                                {
                                    chat = await client.AnalyzeInviteLink(inviteLink, true);
                                }
                                catch (Exception exc)
                                {
                                    if (ex.Message.Contains("FLOOD"))
                                    {
                                        var floodWait = GetFloodWait();
                                        if (floodWait > 0)
                                        {
                                            MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{floodWait}");
                                            isFloodWait[clients.IndexOf(client)] = floodWait;
                                            await Task.Delay(floodWait * 1000);
                                            isFloodWait[clients.IndexOf(client)] = 0;
                                        }
                                        else
                                        {
                                            MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{100}");
                                            await Task.Delay(100 * 1000);
                                        }
                                    }
                                    if (IsForbidden(client, exc))
                                        return false;
                                    try
                                    {
                                        chat = await client.AnalyzeInviteLink(inviteLink, true);
                                    }
                                    catch { return false; }
                                }
                            }
                            catch (Exception exc)
                            {
                                try
                                {
                                    chat = await client.AnalyzeInviteLink(inviteLink);
                                }
                                catch
                                {
                                    return false;
                                }
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
                    var wait = GetFloodWait();
                    if (wait != 0)
                        MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{wait}");
                    IsForbidden(client, ex);
                    return false;
                }
            }
            if (inputPeers[clients.IndexOf(client)] == null)
                return false;
            return true;
        }

        static string GetConfig(string what, Bots client)
        {
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
        public async Task SendReactions(Client client, int countOfMessages, List<string> reactionEmoji, int delay, string inviteLink, int emojiCount)
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
                if (isBanned[clients.IndexOf(client)])
                    break;
                try
                {
                    var messages = await client.Messages_GetHistory(inputPeers[clients.IndexOf(client)], add_offset: sendedReactionsInGroup[inviteLinks.IndexOf(inviteLink)], limit: 100);
                    if (messages.Messages.Count() == 0)
                    {
                        clientsStatus[clients.IndexOf(client)] = true;
                        isBanned[clients.IndexOf(client)] = true;
                        return;
                    }
                    foreach (var messageBase in messages.Messages)
                    {
                        if (isBanned[clients.IndexOf(client)])
                            break;
                        if (messageBase is MessageService messageService || messageBase.From == null || messageBase.From is PeerChannel peerChannel)
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
                                lastMessageInGroup[inviteLinks.IndexOf(inviteLink)]++;
                                reactionsSended++;
                                sendedReactions[clients.IndexOf(client)]++;
                                sendedReactionsInCycle[inviteLinks.IndexOf(inviteLink)]++;
                                sendedReactionsInGroup[inviteLinks.IndexOf(inviteLink)]++;
                                count++;
                                await Task.Delay(delay);
                            }
                            else
                            {
                                var floodWait = GetFloodWait();
                                if (floodWait != 0)
                                {
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
                catch (RpcException rex)
                {
                    var floodWait = GetFloodWait();
                    if (floodWait != 0)
                    {
                        MainInfoLoger.Log($"{client.User.phone} FLOOD_WAIT_{floodWait}");
                        isFloodWait[clients.IndexOf(client)] = floodWait;
                        await Task.Delay(floodWait * 1000);
                        isFloodWait[clients.IndexOf(client)] = 0;
                    }
                }
            }
            clientsStatus[clients.IndexOf(client)] = true;
            isBanned[clients.IndexOf(client)] = true;
        }

        private async Task<List<Reaction>> CheckReactions(Client client, List<string> reactionEmoji)
        {
            reactionEmoji = _emoji;
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
                    for (int i = 0; i < _emojiCount; i++)
                        _reaction.Add(some.reactions[i]);
                }
                else if (fullChannel.full_chat.AvailableReactions is ChatReactionsAll all)
                {
                    if (reactionEmoji.IsNullOrEmpty())
                        reactionEmoji.Add(all_emoji.reactions[random.Next(all_emoji.reactions.Length - 1)].reaction);
                    if (all.flags.HasFlag(ChatReactionsAll.Flags.allow_custom) && client.User.flags.HasFlag(TL.User.Flags.premium))
                    {
                        foreach (var reaction in reactionEmoji)
                        {
                            var reactList = await client.Messages_SearchCustomEmoji(reaction);
                            _reaction.Add(new ReactionCustomEmoji { document_id = GetDocumentId(reaction)
						});;
                        }
                    }
                    else
                    {
                        try
                        {
                            foreach (var reaction in reactionEmoji)
                                _reaction.Add(new ReactionEmoji
                                {
                                    emoticon = all_emoji.reactions.FirstOrDefault(r => r.reaction == reaction).reaction.ToString()
                                });
                        }
                        catch
                        {
                            MainInfoLoger.Log($"Эмодзи {reactionEmoji} не поддерживается в текущей группе, будет выбрано одно из доступных");
                            _reaction.Add(new ReactionEmoji
                            {
                                emoticon = all_emoji.reactions[0].reaction
                            });
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
                await client.Messages_SendReaction(inputPeers[clients.IndexOf(client)], message.ID, reaction: _reaction.ToArray());
            }
            catch (Exception e)
            {
                if (e.Message.Contains("FLOOD"))
                {
                    var floodWait = GetFloodWait();
                    if (floodWait != 0)
                    {
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
            }
        }

        public async Task EditAccount(Client client, string filePath, string firstName, string lastName, string about, string userName, string storyPath, string storyCaption)
        {
            if (!string.IsNullOrEmpty(storyPath))
			{
				try
				{
					var inputFile = await client.UploadFileAsync(storyPath);
					InputMediaUploadedPhoto inputMediaUploadedPhoto = new InputMediaUploadedPhoto() { file = inputFile };
					await client.Stories_SendStory(new InputPeerSelf(), inputMediaUploadedPhoto, new InputPrivacyRule[] { new InputPrivacyValueAllowAll() }, WTelegram.Helpers.RandomLong(), caption: storyCaption);
				}
				catch
				{
					MessageBox.Show("Возникла ошибка изменения фотографии, возможно, путь указан неверно");
				}
			}
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {

                    var inputFile = await client.UploadFileAsync(filePath);
                    await client.Photos_UploadProfilePhoto(inputFile);
                }
                catch
                {
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
			if (!userName.IsNullOrEmpty())
				try
            {
                await client.Account_UpdateUsername(userName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Возникла ошибка изменения юзернейма, возможно, ваш вариант занят или были использованы некорректные символы");
            }
            await Task.Delay(1000);
        }
        public static async Task SkipGroup()
        {
            foreach (var client in clients)
                isBanned[clients.IndexOf(client)] = true;
        }
        long GetDocumentId(string emoji)
        {
            switch (emoji)
            {
                case "\U0001F609": return (long)CustomEmojies.GO;
                case "\U0001F604": return (long)CustomEmojies.TO;
                case "\U0001F60C": return (long)CustomEmojies.LS;
                case "\U0001F61A": return (long)CustomEmojies.STORIS;
                case "\U0001F61B": return (long)CustomEmojies.STORIES;
                case "\U0001F617": return (long)CustomEmojies.INFO;
                case "\U0001F44D": return (long)CustomEmojies.GOTOCHANNEL;
                default: return (long)CustomEmojies.GOTOCHANNEL; // Значение по умолчанию, если нет совпадений.
            }
        }
    }
}
