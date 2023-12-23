using Content.Client.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    internal sealed class SetMenuVisibilityCommand : IConsoleCommand
    {
        public string Command => "menuvis";
        public string Description => Loc.GetString("set-menu-visibility-command-description");
        public string Help => Loc.GetString("set-menu-visibility-command-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!TryParseArguments(shell, args, out var visibility))
                return;

            EntitySystem.Get<VerbSystem>().Visibility = visibility;
        }

        private bool TryParseArguments(IConsoleShell shell, string[] args, out MenuVisibility visibility)
        {
            visibility = MenuVisibility.Default;

            foreach (var arg in args)
            {
                switch (arg.ToLower())
                {
                    case "nofov":
                        visibility |= MenuVisibility.NoFov;
                        break;
                    case "incontainer":
                        visibility |= MenuVisibility.InContainer;
                        break;
                    case "invisible":
                        visibility |= MenuVisibility.Invisible;
                        break;
                    case "all":
                        visibility |= MenuVisibility.All;
                        break;
                    default:
                        shell.WriteError(Loc.GetString("set-menu-visibility-command-error", ("arg", arg)));
                        return false;
                }
            }

            return true;
        }
    }
}
