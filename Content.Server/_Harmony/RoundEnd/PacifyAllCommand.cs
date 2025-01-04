// Credits:
// This code was originally created by DebugOk, deltanedas, and NullWanderer for DeltaV.
// Available at https://github.com/DebugOk/Delta-v/blob/master/Content.Server/DeltaV/RoundEnd/RoundEndSystem.Pacified.cs
// Original PR: https://github.com/DeltaV-Station/Delta-v/pull/350
// Modified by FluffMe on 12.10.2024 with no major changes except the Namespaces and the CVar name.
// Modified and moved by youtissoum on 04.01.2025 to turn into a command.
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Explosion.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Store.Components;
using Robust.Shared.Console;

namespace Content.Server._Harmony.RoundEnd;

[AdminCommand(AdminFlags.Admin)]
public sealed class PacifyAllCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public string Command => "pacifyall";
    public string Description => "Pacify all players permanently.";
    public string Help => string.Empty;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var harmQuery = _entityManager.EntityQueryEnumerator<CombatModeComponent>();
        while (harmQuery.MoveNext(out var uid, out _))
        {
            _entityManager.EnsureComponent<PacifiedComponent>(uid);
        }

        var explosiveQuery = _entityManager.EntityQueryEnumerator<ExplosiveComponent>();
        while (explosiveQuery.MoveNext(out var uid, out _))
        {
            _entityManager.RemoveComponent<ExplosiveComponent>(uid);
        }

        var grenadeQuery = _entityManager.EntityQueryEnumerator<OnUseTimerTriggerComponent>();
        while (grenadeQuery.MoveNext(out var uid, out _))
        {
            _entityManager.RemoveComponent<OnUseTimerTriggerComponent>(uid);
        }

        var flashQuery = _entityManager.EntityQueryEnumerator<FlashComponent>();
        while (flashQuery.MoveNext(out var uid, out _))
        {
            _entityManager.RemoveComponent<FlashComponent>(uid);
        }

        var uplinkQuery = _entityManager.EntityQueryEnumerator<StoreComponent>();
        while (uplinkQuery.MoveNext(out var uid, out _))
        {
            _entityManager.RemoveComponent<StoreComponent>(uid);
        }
    }
}
