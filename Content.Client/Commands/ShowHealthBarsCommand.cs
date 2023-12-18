using Content.Shared.Overlays;
using Robust.Client.Player;
using Robust.Shared.Console;
using System.Linq;

namespace Content.Client.Commands;

public sealed class ShowHealthBarsCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public string Command => "showhealthbars";
    public string Description => "Toggles health bars above mobs.";
    public string Help => $"Usage: {Command} [<DamageContainerId>]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = _playerManager.LocalSession;
        if (player == null)
        {
            shell.WriteLine("You aren't a player.");
            return;
        }

        var playerEntity = player?.AttachedEntity;
        if (!playerEntity.HasValue)
        {
            shell.WriteLine("You do not have an attached entity.");
            return;
        }

        if (!_entityManager.HasComponent<ShowHealthBarsComponent>(playerEntity))
        {
            var showHealthBarsComponent = new ShowHealthBarsComponent
            {
                DamageContainers = args.ToList(),
                NetSyncEnabled = false
            };

            _entityManager.AddComponent(playerEntity.Value, showHealthBarsComponent, true);

            shell.WriteLine($"Enabled health overlay for DamageContainers: {string.Join(", ", args)}.");
            return;
        }
        else
        {
            _entityManager.RemoveComponentDeferred<ShowHealthBarsComponent>(playerEntity.Value);
            shell.WriteLine("Disabled health overlay.");
        }

        return;
    }
}
