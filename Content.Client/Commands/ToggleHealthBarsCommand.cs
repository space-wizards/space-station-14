using Robust.Client.Player;
using Robust.Shared.Console;
using Content.Shared.EntityHealthBar;
using System.Linq;
using Content.Client.EntityHealthHud;

namespace Content.Client.Commands
{
    public sealed class ToggleHealthBarsCommand : IConsoleCommand
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string Command => "togglehealthbars";
        public string Description => "Toggles a health bar above mobs.";
        public string Help => $"Usage: {Command} [<DamageContainerId>]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = _playerManager.LocalPlayer;
            if (player == null)
            {
                shell.WriteLine("You aren't a player.");
                return;
            }

            var playerEntity = player?.ControlledEntity;
            if (playerEntity == null)
            {
                shell.WriteLine("You do not have an attached entity.");
                return;
            }

            var showHealthBarsSystem = EntitySystem.Get<ShowHealthBarsSystem>();
            if (!_entityManager.HasComponent<ShowHealthBarsComponent>(playerEntity))
            {
                var showHealthBarsComponent = _entityManager.AddComponent<ShowHealthBarsComponent>((EntityUid) playerEntity);
                showHealthBarsComponent.DamageContainers = args.ToList();
                showHealthBarsSystem.ApplyOverlays(showHealthBarsComponent);
                shell.WriteLine($"Enabled health overlay for DamageContainers: {string.Join(", ", args)}.");
                return;
            }

            _entityManager.RemoveComponent<ShowHealthBarsComponent>((EntityUid) playerEntity);
            showHealthBarsSystem.RemoveOverlay();
            shell.WriteLine("Disabled health overlay.");
            return;
        }
    }
}
