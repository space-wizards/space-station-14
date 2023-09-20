using Content.Shared.Broke;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DispenseOnHit;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Random;

namespace Content.Server.VendingMachines;

public sealed partial class VendingMachineSystem
{
    private void OnBreak(EntityUid uid, BrokeComponent component, BreakageEventArgs eventArgs)
    {
        component.IsBroken = true;

        UpdateVisualState(uid);
    }

    private void OnDamage(EntityUid uid, BrokeComponent brokeComponent, DamageChangedEvent args)
    {
        if (!TryComp<DispenseOnHitComponent>(uid, out var dispenseComponent))
            return;

        if (brokeComponent.IsBroken || dispenseComponent.CoolingDown ||
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

            EjectRandom(uid, throwItem: true, forceEject: true, ejectComponent);
        }
    }

}
