using Content.Client.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    internal sealed class SetMenuVisibilityCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        // ReSharper disable once StringLiteralTypo
        public string Command => "menuvis";
        public string Description => Loc.GetString("set-menu-visibility-command-description");
        public string Help => Loc.GetString("set-menu-visibility-command-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!TryParseArguments(shell, args, out var visibility))
                return;

            _entitySystemManager.GetEntitySystem<VerbSystem>().Visibility = visibility;
        }

        private bool TryParseArguments(IConsoleShell shell, string[] args, out MenuVisibility visibility)
        {
            visibility = MenuVisibility.Default;

            foreach (var arg in args)
            {
                switch (arg.ToLower())
                {
                    // ReSharper disable once StringLiteralTypo
                    case "nofov":
                        visibility |= MenuVisibility.NoFov;
                        break;
                    // ReSharper disable once StringLiteralTypo
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
