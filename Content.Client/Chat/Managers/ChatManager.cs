using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Shared.Network;

namespace Content.Client.Chat.Managers;

internal sealed class ChatManager : IChatManager
{
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly INetManager _net = default!;

    public void Initialize()
    {
        _net.RegisterNetMessage<RequestChatMessage>();
    }

    public void SendAdminAlert(string message)
    {
        // See server-side manager. This just exists for shared code.
    }

    public void SendAdminAlert(EntityUid player, string message)
    {
        // See server-side manager. This just exists for shared code.
    }

    public void SendMessage(string text, ChatSelectChannel channel)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (channel == ChatSelectChannel.Console)
            _consoleHost.ExecuteCommand(text);
        else
            _net.ClientSendMessage(new RequestChatMessage { Text = text, Channel = channel, });
    }
}
