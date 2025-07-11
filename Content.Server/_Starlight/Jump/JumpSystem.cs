using Content.Server.Administration.Systems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction.Conditions;
using Content.Shared._Starlight.Actions.Jump;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map;

namespace Content.Server._Starlight.Jump;
public sealed class JumpSystem : SharedJumpSystem
{
    [Dependency] private readonly GasTankSystem _gasTank = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly StarlightEntitySystem _entities = default!;

    protected override bool TryReleaseGas(Entity<JumpComponent> ent, ref JetJumpActionEvent args)
    {
        if (!_entities.TryEntity<GasTankComponent>(ent, out var gastank) || gastank.Comp.Air.TotalMoles < args.MoleUsage)
            return false;

        var usedAir = _gasTank.RemoveAir(gastank, args.MoleUsage);

        var gas = _atmos.GetTileMixture((ent.Owner, null), true) ?? new();
        if (gas != null && !gas.Immutable && usedAir != null)
            _atmos.Merge(gas, usedAir);

        return true;
    }
}
