using Content.Shared.Emp;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedBatterySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(Entity<BatteryComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        UseCharge(ent.AsNullable(), args.EnergyConsumption);
        // Apply a cooldown to the entity's self recharge if needed to avoid it immediately self recharging after an EMP.
        TrySetChargeCooldown(ent.Owner);
    }

    /// <summary>
    /// Changes the battery's charge by the given amount.
    /// A positive value will add charge, a negative value will remove charge.
    /// </summary>
    /// <returns>The actually changed amount.</returns>
    public virtual float ChangeCharge(Entity<BatteryComponent?> ent, float amount)
    {
        return 0f;
    }

    /// <summary>
    /// Removes the given amount of charge from the battery.
    /// </summary>
    /// <returns>The actually changed amount.</returns>
    public virtual float UseCharge(Entity<BatteryComponent?> ent, float amount)
    {
        return 0f;
    }

    /// <summary>
    /// If sufficient charge is available on the battery, use it. Otherwise, don't.
    /// Always returns false on the client.
    /// </summary>
    /// <returns>If the full amount was able to be removed.</returns>
    public virtual bool TryUseCharge(Entity<BatteryComponent?> ent, float amount)
    {
        return false;
    }

    /// <summary>
    /// Sets the battery's charge.
    /// </summary>
    public virtual void SetCharge(Entity<BatteryComponent?> ent, float value) { }

    /// <summary>
    /// Sets the battery's maximum charge.
    /// </summary>
    public virtual void SetMaxCharge(Entity<BatteryComponent?> ent, float value) { }

    /// <summary>
    /// Checks if the entity has a self recharge and puts it on cooldown if applicable.
    /// Uses the cooldown time given in the component.
    /// </summary>
    public virtual void TrySetChargeCooldown(Entity<BatterySelfRechargerComponent?> ent) { }

    /// <summary>
    /// Puts the entity's self recharge on cooldown for the specified time.
    /// </summary>
    public virtual void SetChargeCooldown(Entity<BatterySelfRechargerComponent?> ent, TimeSpan cooldown) { }
}
