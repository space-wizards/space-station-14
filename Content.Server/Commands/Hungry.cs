#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Administration;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;

namespace Content.Server.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public class Hungry : IClientCommand
    {
        public string Command => "hungry";
        public string Description => "Makes you hungry.";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (player == null)
            {
                shell.SendText(player, "You cannot use this command unless you are a player.");
                return;
            }

            if (player.AttachedEntity == null)
            {
                shell.SendText(player, "You cannot use this command without an entity.");
                return;
            }

            if (!player.AttachedEntity.TryGetComponent(out HungerComponent? hunger))
            {
                shell.SendText(player, $"Your entity does not have a {nameof(HungerComponent)} component.");
                return;
            }

            var hungryThreshold = hunger.HungerThresholds[HungerThreshold.Starving];
            hunger.CurrentHunger = hungryThreshold;
        }
    }
}
