using Starksoft.Net.Proxy;
using System.Threading.Tasks;
using TL;
using WTelegram;
using ChatBot.Pages.DialogWindows;
using System.Collections.Generic;
using System.Windows;

namespace ChatBot.Core;

internal class TelegramClient
{
    public static readonly Socks5ProxyClient[] socks5 = new Socks5ProxyClient[] //вынести
    {
         new Socks5ProxyClient("138.36.95.240", 50101, "legenadarypigeon", "XAC8nIEFGU"),
         new Socks5ProxyClient("45.152.177.6", 50101, "legenadarypigeon", "XAC8nIEFGU"),
    };

    static string[] phones = new string[] //вынести
    {
        "+79393498320",
        "+79196431832"
    };

    public static async void Start()
    {
        List<Client> clients = new() //вынести
        {
            GetUserSocks5Proxy("session1", socks5[0], phones[0]),
            GetUserSocks5Proxy("session2", socks5[2], phones[1]),
        };

        foreach (var client in clients)
        {
            await client.LoginUserIfNeeded();
            await SendMessages(client);
        }
    }

    static Client GetUserSocks5Proxy(string sessionPathname, Socks5ProxyClient proxy, string phone)
    {
        var client = new Client(what => GetConfig(what, sessionPathname, phone));
        client.TcpHandler = async (address, port) =>
        {
            return proxy.CreateConnection(address, port);
        };

        return client;
    }

    static async Task SendMessages(Client client)
    {
        var chat = await client.AnalyzeInviteLink("https://t.me/+RlcBFp-EB501MmY6", false); //Вынести
        var inputChat = new InputPeerChat(chat.ID);
        var fullChat = await client.GetFullChat(inputChat);

        foreach (long id in fullChat.users.Keys)
        {
            fullChat.users.TryGetValue(id, out var user);
            var myMessage = await client.SendMessageAsync(chat, $"@{user.username}");
            await client.Messages_EditMessage(inputChat, id: myMessage.id, message: "Горячие мамки в 300 метрах от вас"); //Вынести
        }
    }

    static string GetConfig(string what, string sessionPathname, string phone)
    {
        switch (what)
        {
            case "session_key": return "1f397d126d7c2c95299c4a21320e9e02";
            case "api_id": return "27114840";
            case "api_hash": return "1f397d126d7c2c95299c4a21320e9e02";
            case "phone_number": return phone;
            case "verification_code":
                string code = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    VerificationCodeDialogPage verificationCode = new();
                    verificationCode.ShowDialog();
                    code = verificationCode.Code;
                });
                return code;
            case "first_name": return "John";
            case "session_pathname":
                return sessionPathname;
            default:
                return null;
        }
    }
}
