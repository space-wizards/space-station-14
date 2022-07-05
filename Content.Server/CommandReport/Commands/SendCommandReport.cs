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

namespace Content.Server.CommandReport.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SendNukeCodesCommand : IConsoleCommand
    {
        public string Command => "sendcommandreport";
        public string Description => "Send a command report to a communications console or through the radio.";
        public string Help => $"{Command} <broadcast_message_to_radio> <message>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteError("Not enough arguments! Need at least 2.");
                return;
            }

            EntitySystem.Get<CommandReportSystem>().SendCommandReport(bool.Parse(args[0]), args[1]);

            shell.WriteLine("Sent!");
        }
    }
}
