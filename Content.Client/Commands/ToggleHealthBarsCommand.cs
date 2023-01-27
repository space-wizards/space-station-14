using Robust.Client.Player;
using Robust.Shared.Console;
using Content.Shared.EntityHealthBar;

namespace Content.Client.Commands
{
    public sealed class ToggleHealthBarsCommand : IConsoleCommand
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string Command => "togglehealthbars";
        public string Description => "Toggles a health bar above mobs.";
        public string Help => $"Usage: {Command}";

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

            if (!_entityManager.TryGetComponent<ShowHealthBarsComponent>(playerEntity, out var glassComp))
            {
                _entityManager.AddComponent<ShowHealthBarsComponent>((EntityUid) playerEntity);
                shell.WriteLine("Enabled health overlay.");
                return;
            }

            _entityManager.RemoveComponent<ShowHealthBarsComponent>((EntityUid) playerEntity);
            shell.WriteLine("Disabled health overlay.");
            return;
        }
    }
}
