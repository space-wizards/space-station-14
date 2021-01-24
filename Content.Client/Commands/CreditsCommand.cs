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

        public void Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            new CreditsWindow().Open();
        }
    }
}
