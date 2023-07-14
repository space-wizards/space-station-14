using Content.Shared.Broke;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DispenseOnHit;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.VendingMachines;

public sealed class VendingMachineDamageSystem : EntitySystem
{
    [Dependency] private readonly VendingMachineSystem _machineSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly VendingMachineVisualStateSystem _visualStateSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrokeComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<BrokeComponent, DamageChangedEvent>(OnDamage);
    }

    private void OnBreak(EntityUid uid, BrokeComponent component, BreakageEventArgs eventArgs)
    {
        component.Broken = true;

        _visualStateSystem.UpdateVisualState(uid);
    }

    private void OnDamage(EntityUid uid, BrokeComponent brokeComponent, DamageChangedEvent args)
    {
        if (!TryComp<DispenseOnHitComponent>(uid, out var dispenseComponent))
            return;

        if (brokeComponent.Broken || dispenseComponent.CoolingDown ||
            dispenseComponent.Chance == null || args.DamageDelta == null)
            return;

        if (args.DamageIncreased && args.DamageDelta.Total >= dispenseComponent.Threshold &&
            _random.Prob(dispenseComponent.Chance.Value))
        {
            if (dispenseComponent.Delay > TimeSpan.Zero)
                dispenseComponent.CoolingDown = true;

            dispenseComponent.Cooldown = _timing.CurTime + dispenseComponent.Delay;

            if (!TryComp<VendingMachineEjectComponent>(uid, out var ejectComponent))
                return;

            _machineSystem.EjectRandom(uid, throwItem: true, forceEject: true, ejectComponent);
        }
    }

}
