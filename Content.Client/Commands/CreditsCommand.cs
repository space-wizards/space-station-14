using Content.Client.Credits;
using Content.Client.UserInterface;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    [UsedImplicitly, AnyCommand]
    public sealed class CreditsCommand : IConsoleCommand
    {
        public string Command => "credits";
        public string Description => "Opens the credits window";
        public string Help => "credits";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            new CreditsWindow().Open();
        }
    }
}
