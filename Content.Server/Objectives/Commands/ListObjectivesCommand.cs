using System.Linq;
using Content.Server.Administration;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Objectives.Commands
{
    [AdminCommand(AdminFlags.Logs)]
    public sealed class ListObjectivesCommand : LocalizedCommands
    {
        public override string Command => "lsobjectives";
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            IPlayerData? data;
            if (args.Length == 0 && player != null)
            {
                data = player.Data;
            }
            else if (player == null || !IoCManager.Resolve<IPlayerManager>().TryGetPlayerDataByUsername(args[0], out data))
            {
                shell.WriteLine(LocalizationManager.GetString("cmd-lsobjectives-player"));
                return;
            }

            var mind = data.ContentData()?.Mind;
            if (mind == null)
            {
                shell.WriteLine(LocalizationManager.GetString("cmd-lsobjectives-mind"));
                return;
            }

            shell.WriteLine($"Objectives for player {data.UserId}:");
            var objectives = mind.AllObjectives.ToList();
            if (objectives.Count == 0)
            {
                shell.WriteLine("None.");
            }
            for (var i = 0; i < objectives.Count; i++)
            {
                shell.WriteLine($"- [{i}] {objectives[i].Conditions[0].Title}");
            }

        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var playerMgr = IoCManager.Resolve<IPlayerManager>();
                var options = playerMgr.ServerSessions.Select(c => c.Name).OrderBy(c => c).ToArray();
                return CompletionResult.FromHintOptions(options, LocalizationManager.GetString("cmd-lsobjectives-hint"));
            }

            return CompletionResult.Empty;
        }
    }
}
