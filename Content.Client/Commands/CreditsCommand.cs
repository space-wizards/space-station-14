using Content.Client.UserInterface;
using JetBrains.Annotations;
using Robust.Client.Console;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    public sealed class CreditsCommand : IClientCommand
    {
        public string Command => "credits";
        public string Description => "Opens the credits window";
        public string Help => "credits";

        public bool Execute(IClientConsoleShell shell, string[] args)
        {
            new CreditsWindow().Open();
            return false;
        }
    }
}
