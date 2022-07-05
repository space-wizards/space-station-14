using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Shared.Chat;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.CommandReport
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SendNukeCodesCommand : IConsoleCommand
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        public string Command => "sendcommandreport";
        public string Description => "Send a command report to a communications console or through the radio.";
        public string Help => $"{Command} <broadcast_to_radio> <message>";

        private const string AnnouncementSound = "/Audio/Announcements/commandreport.ogg";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            // Such copypaste code... So sorry...
            var chat = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();

            if (args.Length < 2)
            {
                shell.WriteError("Not enough arguments! Need at least 2.");
                return;
            }

            var message = args[1];
            var messageWrap = Loc.GetString("command-reports-sender-announcement-wrap-message");

            SoundSystem.Play(AnnouncementSound, Filter.Broadcast(), AudioParams.Default.WithVolume(-2f));

            _chatManager.ChatMessageToAll(ChatChannel.Radio, args[0] == "true" ? message : Loc.GetString("command-reports-confidential-announcement-message"), messageWrap, colorOverride: Color.Gold);

            if (args[0] == "false")
            {
                // some copypaste code from NukeCodeSystem.cs, to the person making fax, don't forget to convert this too!
                var consoles = EntityManager.EntityQuery<CommunicationsConsoleComponent>();
                foreach (var console in consoles)
                {
                    if (!EntityManager.TryGetComponent((console).Owner, out TransformComponent? transform))
                        continue;

                    var consolePos = transform.MapPosition;
                    var paper = EntityManager.SpawnEntity("Paper", consolePos);
                }
            }

            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Command report sent: {message}");

            shell.WriteLine("Sent!");
        }
    }
}
