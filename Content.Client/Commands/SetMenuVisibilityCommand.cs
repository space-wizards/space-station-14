using Content.Client.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Client.Commands
{
    [UsedImplicitly]
    internal sealed class SetMenuVisibilityCommand : IConsoleCommand
    {
        public const string CommandName = "menuvis";

        public string Command => CommandName;
        public string Description => "Set restrictions about what entities to show on the entity context menu.";
        public string Help => $"Usage: {Command} [NoFoV] [InContainer] [Invisible] [All]";

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
                        shell.WriteLine($"Unknown visibility argument '{arg}'. Only 'NoFov', 'InContainer', 'Invisible' or 'All' are valid. Provide no arguments to set to default.");
                        return false;
                }
            }

            return true;
        }
    }
}
