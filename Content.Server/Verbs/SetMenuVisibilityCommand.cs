using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server.Verbs
{
    [AdminCommand(AdminFlags.Debug)]
    public class SetMenuVisibilityCommand : IConsoleCommand
    {
        public const string CommandName = "menuvis";

        public string Command => CommandName;
        public string Description => "Set restrictions about what entities to show on the entity context menu.";
        public string Help => $"Usage: {Command} [NoFoV] [InContainer] [Invisible] [All]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (!TryParseArguments(shell, args, out var visibility))
                return;

            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You need to be a player to use this command.");
                return;
            }

            EntitySystem.Get<VerbSystem>().SetMenuVisibility(player, visibility);
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
