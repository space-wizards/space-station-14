using Content.Client.UserInterface;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Console;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    public sealed class CreditsCommand : IConsoleCommand
    {
        public string Command => "credits";
        public string Description => "Opens the credits window";
        public string Help => "credits";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            new CreditsWindow().Open();
            return false;
        }
    }
}
