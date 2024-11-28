using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Events;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;

namespace Content.Server.Jellid.Systems
{
    public sealed class JellidDrawSystem : EntitySystem
    {
        [Dependency] private readonly BatterySystem _battery = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;

        public float DrainAmount;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<JellidComponent, ChargeChangedEvent>(OnChargeChanged);
        }
        private void OnChargeChanged(Entity<JellidComponent> entity, ref ChargeChangedEvent args)
        {
            float DamageCharge = 120f;
            if (TryComp<BatteryComponent>(entity.Owner, out var battery) &&
                battery.CurrentCharge < DamageCharge)
            {
                if (Charging)
                    {
                    return;
                    }
                else
                {
                 var damage = new DamageSpecifier
                   {
                  DamageDict = { ["Slash"] = 1.5f }
                   };
                _damageable.TryChangeDamage(entity.Owner, damage, origin: entity.Owner);
                }
            }
        }

public override void Update(float frameTime)
{
    base.Update(frameTime);

    var playerQuery = EntityQueryEnumerator<HandsComponent>();
    while (playerQuery.MoveNext(out var playerUid, out var handsComponent))
    {
        if (!HasComp<JellidComponent>(playerUid))
            continue;

        if (handsComponent.ActiveHand?.HeldEntity is not EntityUid heldItem)
            continue;

        if (!TryComp<BatteryComponent>(heldItem, out var containerBattery))
            continue;

        if (!TryComp<BatteryComponent>(playerUid, out var internalBattery))
            continue;

        // Drain power from the held item's battery into the player's internal battery
        DrainPower(containerBattery, internalBattery);
    }
}

private void DrainPower(BatteryComponent containerBattery, BatteryComponent internalBattery)
{
    // Determine how much charge can be drained
    float Offset = 0.5f;
    var drainAmount = Math.Min(containerBattery.CurrentCharge, Offset);

    // If there's charge to drain
    if (drainAmount > 0)
    {
        // Directly use the BatterySystem to change the charge values
        _battery.SetCharge(containerBattery.Owner, containerBattery.CurrentCharge - drainAmount, containerBattery);
        _battery.SetCharge(internalBattery.Owner, internalBattery.CurrentCharge + drainAmount, internalBattery);
    }

}

    public bool Charging
    {
        get
        {
        return DrainAmount > 0;
        }
    }

    }
}
