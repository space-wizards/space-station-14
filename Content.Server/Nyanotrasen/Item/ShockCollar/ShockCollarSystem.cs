using Content.Shared.Mobs.Components;
using Content.Shared.Timing;
using Content.Server.Explosion.EntitySystems; // Why is trigger under explosions by the way? Even doors already use it.
using Content.Server.Electrocution;
using Robust.Shared.Containers;

namespace Content.Server.ShockCollar;

public sealed partial class ShockCollarSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShockCollarComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(EntityUid uid, ShockCollarComponent component, TriggerEvent args)
    {
        if (!_container.TryGetContainingContainer(uid, out var container)) // Try to get the entity directly containing this
            return;

        var containerEnt = container.Owner;

        if (!HasComp<MobStateComponent>(containerEnt)) // If it's not a mob we don't care
            return;

        // DeltaV: prevent clocks from instantly killing people
        if (TryComp<UseDelayComponent>(uid, out var useDelay)
            && !_useDelay.TryResetDelay((uid, useDelay), true))
            return;

        _electrocutionSystem.TryDoElectrocution(containerEnt, null, 5, TimeSpan.FromSeconds(2), true);
    }
}

