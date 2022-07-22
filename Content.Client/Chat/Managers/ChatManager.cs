using Content.Client.Administration.Managers;
using Content.Client.Ghost;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.Utility;

namespace Content.Client.Chat.Managers
{
    internal sealed class ChatManager : IChatManager
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IClientAdminManager _adminMgr = default!;

        private ISawmill _sawmill = default!;

        public void Initialize()
        {
            _sawmill = Logger.GetSawmill("chat");
            _sawmill.Level = LogLevel.Info;
        }

        public bool IsGhost => _playerManager.LocalPlayer?.ControlledEntity is {} uid &&
                               uid.IsValid() &&
                               _entityManager.HasComponent<GhostComponent>(uid);

        public void SendMessage(ReadOnlyMemory<char> text, ChatSelectChannel channel)
        {
            var str = text.ToString();
            switch (channel)
            {
                case ChatSelectChannel.Console:
                    // run locally
                    _consoleHost.ExecuteCommand(text.ToString());
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
                    if (IsGhost)
                        goto case ChatSelectChannel.Local;

                    if (_adminMgr.HasFlag(AdminFlags.Admin))
                        _consoleHost.ExecuteCommand($"dsay \"{CommandParsing.Escape(str)}\"");
                    else
                        _sawmill.Warning("Tried to speak on deadchat without being ghost or admin.");
                    break;

                case ChatSelectChannel.Radio:
                    _consoleHost.ExecuteCommand($"say \";{CommandParsing.Escape(str)}\"");
                    break;

                case ChatSelectChannel.Local:
                    _consoleHost.ExecuteCommand($"say \"{CommandParsing.Escape(str)}\"");
                    break;

                case ChatSelectChannel.Whisper:
                    _consoleHost.ExecuteCommand($"whisper \"{CommandParsing.Escape(str)}\"");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }
    }
}
