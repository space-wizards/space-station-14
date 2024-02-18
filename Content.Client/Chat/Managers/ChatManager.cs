using Content.Client.Administration.Managers;
using Content.Client.Ghost;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Shared.Utility;

namespace Content.Client.Chat.Managers
{
    internal sealed class ChatManager : IChatManager
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IClientAdminManager _adminMgr = default!;
        [Dependency] private readonly IEntitySystemManager _systems = default!;

        private ISawmill _sawmill = default!;

        public void Initialize()
        {
            _sawmill = Logger.GetSawmill("chat");
            _sawmill.Level = LogLevel.Info;
        }

        public void SendMessage(string text, ChatSelectChannel channel)
        {
            switch (channel)
            {
                case ChatSelectChannel.Console:
                    // run locally
                    _consoleHost.ExecuteCommand(text);
                    break;

                case ChatSelectChannel.OOC:
                    _consoleHost.ExecuteCommand($"ooc \"{CommandParsing.Escape(text)}\"");
                    break;

                case ChatSelectChannel.Admin:
                    _consoleHost.ExecuteCommand($"asay \"{CommandParsing.Escape(text)}\"");
                    break;

                case ChatSelectChannel.Dead:
                    if (_systems.GetEntitySystemOrNull<GhostSystem>() is {IsGhost: true})
                        return;

                    if (_adminMgr.HasFlag(AdminFlags.Admin))
                        _consoleHost.ExecuteCommand($"dsay \"{CommandParsing.Escape(text)}\"");
                    else
                        _sawmill.Warning("Tried to speak on dead chat without being ghost or admin.");
                    break;
                case ChatSelectChannel.None:
                case ChatSelectChannel.Local:
                case ChatSelectChannel.Whisper:
                case ChatSelectChannel.Radio:
                case ChatSelectChannel.Emotes:
                case ChatSelectChannel.LOOC:
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }
    }
}
