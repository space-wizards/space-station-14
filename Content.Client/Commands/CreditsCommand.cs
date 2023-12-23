using Content.Client.Credits;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    [UsedImplicitly, AnyCommand]
    public sealed class CreditsCommand : IConsoleCommand
    {
        public string Command => "credits";
        public string Description => Loc.GetString("credits-command-description");
        public string Help => Loc.GetString("credits-command-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            new CreditsWindow().Open();
        }
    }
}
