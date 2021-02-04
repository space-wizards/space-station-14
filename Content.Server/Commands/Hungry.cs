#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Administration;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;

namespace Content.Server.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public class Hungry : IConsoleCommand
    {
        public string Command => "hungry";
        public string Description => "Makes you hungry.";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You cannot use this command unless you are a player.");
                return;
            }

            if (player.AttachedEntity == null)
            {
                shell.WriteLine("You cannot use this command without an entity.");
                return;
            }

            if (!player.AttachedEntity.TryGetComponent(out HungerComponent? hunger))
            {
                shell.WriteLine($"Your entity does not have a {nameof(HungerComponent)} component.");
                return;
            }

            var hungryThreshold = hunger.HungerThresholds[HungerThreshold.Starving];
            hunger.CurrentHunger = hungryThreshold;
        }
    }
}
