using Content.Client.Administration.Managers;
using Content.Client.Ghost;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Shared.Utility;

namespace Content.Client.Chat.Managers;

internal sealed class ChatManager : IChatManager
{
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly IClientAdminManager _adminMgr = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    private ISawmill _sawmill = default!;
    public event Action? PermissionsUpdated;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("chat");
        _sawmill.Level = LogLevel.Info;
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
        var str = text.ToString();
        switch (channel)
        {
            case ChatSelectChannel.Console:
                // run locally
                _consoleHost.ExecuteCommand(text);
                break;

            case ChatSelectChannel.LOOC:
                _consoleHost.ExecuteCommand($"looc \"{CommandParsing.Escape(str)}\"");
                break;

            case ChatSelectChannel.OOC:
                _consoleHost.ExecuteCommand($"ooc \"{CommandParsing.Escape(str)}\"");
                break;

            case ChatSelectChannel.Admin:
                _consoleHost.ExecuteCommand($"asay \"{CommandParsing.Escape(str)}\"");
                break;

            case ChatSelectChannel.Emotes:
                _consoleHost.ExecuteCommand($"me \"{CommandParsing.Escape(str)}\"");
                break;

            case ChatSelectChannel.Dead:
                if (_systems.GetEntitySystemOrNull<GhostSystem>() is {IsGhost: true})
                    goto case ChatSelectChannel.Local;

                if (_adminMgr.HasFlag(AdminFlags.Admin))
                    _consoleHost.ExecuteCommand($"dsay \"{CommandParsing.Escape(str)}\"");
                else if (_systems.GetEntitySystemOrNull<GhostSystem>() is {IsGhostBarPatron: true})
                    _consoleHost.ExecuteCommand($"gbsay \"{CommandParsing.Escape(str)}\"");
                else
                    _sawmill.Warning("Tried to speak on deadchat without being ghost or admin.");
                break;

            // TODO sepearate radio and say into separate commands.
            case ChatSelectChannel.Radio:
            case ChatSelectChannel.Local:
                _consoleHost.ExecuteCommand($"say \"{CommandParsing.Escape(str)}\"");
                break;

            case ChatSelectChannel.Whisper:
                _consoleHost.ExecuteCommand($"whisper \"{CommandParsing.Escape(str)}\"");
                break;

            case ChatSelectChannel.CollectiveMind:
                _consoleHost.ExecuteCommand($"cmsay \"{CommandParsing.Escape(str)}\"");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
        }
    }

    public void UpdatePermissions()
    {
        PermissionsUpdated?.Invoke();
    }
}
