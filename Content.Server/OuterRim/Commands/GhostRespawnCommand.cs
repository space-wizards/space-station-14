using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Server.OuterRim.Commands;

[AnyCommand()]
public sealed class GhostRespawnCommand : IConsoleCommand
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public string Command => "ghostrespawn";
    public string Description => "";
    public string Help => "";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is null)
            return;
        if (shell.Player.AttachedEntity is null)
            return;
        if (!_entityManager.TryGetComponent<GhostComponent>(shell.Player.AttachedEntity, out var ghost))
            return;

        var time = ( _gameTiming.CurTime - ghost.TimeOfDeath);
        var respawnTime = _configurationManager.GetCVar(CCVars.RespawnTime);

        if (respawnTime > time.TotalSeconds)
            return;

        var gameTicker = _entityManager.EntitySysManager.GetEntitySystem<GameTicker>();

        gameTicker.Respawn((IPlayerSession)shell.Player);
    }
}
