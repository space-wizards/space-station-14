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

    private void OnEmpPulse(Entity<BatteryComponent> entity, ref EmpPulseEvent args)
    {
        args.Affected = true;
        UseCharge(entity, args.EnergyConsumption, entity.Comp);
        // Apply a cooldown to the entity's self recharge if needed to avoid it immediately self recharging after an EMP.
        TrySetChargeCooldown(entity);
    }

    public virtual float UseCharge(EntityUid uid, float value, BatteryComponent? battery = null)
    {
        return 0f;
    }

    public virtual void SetMaxCharge(EntityUid uid, float value, BatteryComponent? battery = null) { }

    public virtual float ChangeCharge(EntityUid uid, float value, BatteryComponent? battery = null)
    {
        return 0f;
    }

    /// <summary>
    /// Checks if the entity has a self recharge and puts it on cooldown if applicable.
    /// </summary>
    public virtual void TrySetChargeCooldown(EntityUid uid, float value = -1) { }

    public virtual bool TryUseCharge(EntityUid uid, float value, BatteryComponent? battery = null)
    {
        return false;
    }
}
