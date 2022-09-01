using Content.Server.Body.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    public sealed class PrayCommand : IConsoleCommand
    {
        public string Command => "pray";
        public string Description => Loc.GetString("prayer-command-description");
        public string Help => Loc.GetString("prayer-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            Logger.Debug(argStr);
        }
    }
}
