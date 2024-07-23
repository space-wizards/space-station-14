using Content.Shared.Overlays;
using Robust.Client.Player;
using Robust.Shared.Console;
using System.Linq;

namespace Content.Client.Commands;

public sealed class ShowHealthBarsCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "showhealthbars";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = _playerManager.LocalSession;
        if (player == null)
        {
            shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error-not-player"));
            return;
        }

        var playerEntity = player?.AttachedEntity;
        if (!playerEntity.HasValue)
        {
            shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-error-no-entity"));
            return;
        }

        if (!_entityManager.HasComponent<ShowHealthBarsComponent>(playerEntity))
        {
            var showHealthBarsComponent = new ShowHealthBarsComponent
            {
                DamageContainers = args.ToList(),
                HealthStatusIcon = null,
                NetSyncEnabled = false
            };

            _entityManager.AddComponent(playerEntity.Value, showHealthBarsComponent);

            shell.WriteLine(LocalizationManager.GetString($"cmd-{Command}-notify-enabled", ("args", string.Join(", ", args))));
            return;
        }
        else
        {
            _entityManager.RemoveComponentDeferred<ShowHealthBarsComponent>(playerEntity.Value);
            shell.WriteLine(LocalizationManager.GetString($"cmd-{Command}-notify-disabled"));
        }

        return;
    }
}
