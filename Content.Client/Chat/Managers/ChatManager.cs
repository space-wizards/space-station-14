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
            var str = text.ToString();

            str = FilterAccidentalInput(str, ref channel);

            switch (channel)
            {
                case ChatSelectChannel.Console:
                    // run locally
                    _consoleHost.ExecuteCommand(FilterAccidentalInput(text, ref channel));
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

                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }

        public string FilterAccidentalInput(string input, ref ChatSelectChannel channel)
        {
            string _input = input.ToLower().Trim();

            if (_input[0] == 't' && _input.Length >= 2)
            {
                switch (_input[1])
                {
                    case '/':
                        channel = ChatSelectChannel.Console;
                        return input.Substring(2);
                    case '(':
                        channel = ChatSelectChannel.LOOC;
                        return input.Substring(2);
                    case '[':
                        channel = ChatSelectChannel.OOC;
                        return input.Substring(2);
                    case ']':
                        channel = ChatSelectChannel.Admin;
                        return input.Substring(2);
                    case '@':
                        channel = ChatSelectChannel.Emotes;
                        return input.Substring(2);
                    case '\\':
                        channel = ChatSelectChannel.Dead;
                        return input.Substring(2);
                    case '>':
                        channel = ChatSelectChannel.Local;
                        return input.Substring(2);
                    case ';':
                        channel = ChatSelectChannel.Radio;
                        return input.Substring(1);
                    case ':':
                        channel = ChatSelectChannel.Radio;
                        return input.Substring(1);
                    case ',':
                        channel = ChatSelectChannel.Whisper;
                        return input.Substring(2);
                    default:
                        return input;
                }
            }

            return input;
        }
    }
}
