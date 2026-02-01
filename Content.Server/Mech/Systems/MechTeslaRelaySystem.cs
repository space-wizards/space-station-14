using Content.Server.Power.Components;
using Content.Shared.APC;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Module.Components;
using Content.Shared.Mech.Systems;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Aggregates recharge contributions from Tesla relay mech modules by detecting powered APCs near mechs
/// and adding their configured chargeRate into the mech's per-tick recharge accumulator.
/// </summary>
public sealed class MechTeslaRelaySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMechSystem _mech = default!;

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MechComponent>();
        while (query.MoveNext(out var mechUid, out var mech))
        {
            if (!TryComp<MechEnergyAccumulatorComponent>(mechUid, out var acc))
                acc = EnsureComp<MechEnergyAccumulatorComponent>(mechUid);

            foreach (var module in mech.ModuleContainer.ContainedEntities)
            {
                if (!TryComp<MechGeneratorModuleComponent>(module, out var gen))
                    continue;

                if (gen.GenerationType != MechGenerationType.TeslaRelay)
                    continue;

                var telem = EnsureComp<MechEnergyAccumulatorComponent>(module);
                var radius = gen.Tesla?.Radius ?? 0f;
                var rate = gen.Tesla?.ChargeRate ?? 0f;
                telem.Max = rate;
                telem.Current = 0f;
                if (radius <= 0f || rate <= 0f)
                    continue;

                if (IsNearPoweredApc(mechUid, radius))
                {
                    acc.PendingRechargeRate += rate;
                    telem.Current = rate;
                }
            }

            _mech.UpdateMechUi(mechUid);
        }
    }

    private bool IsNearPoweredApc(EntityUid mech, float radius)
    {
        var apcs = new HashSet<Entity<ApcComponent>>();
        _lookup.GetEntitiesInRange(Transform(mech).Coordinates, radius, apcs);
        foreach (var apc in apcs)
        {
            if (apc.Comp.MainBreakerEnabled && apc.Comp.LastExternalState != ApcExternalPowerState.None)
                return true;
        }

        return false;
    }
}
