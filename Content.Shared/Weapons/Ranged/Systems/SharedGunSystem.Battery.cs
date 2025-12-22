using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Projectiles;
using Content.Shared.Power;
using Content.Shared.PowerCell;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected virtual void InitializeBattery()
    {
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ComponentStartup>(OnBatteryStartup);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, TakeAmmoEvent>(OnBatteryTakeAmmo);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, GetAmmoCountEvent>(OnBatteryAmmoCount);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ExaminedEvent>(OnBatteryExamine);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, DamageExamineEvent>(OnBatteryDamageExamine);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ChargeChangedEvent>(OnChargeChanged);
    }

    private void OnBatteryExamine(Entity<BatteryAmmoProviderComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("gun-battery-examine", ("color", AmmoExamineColor), ("count", ent.Comp.Shots)));
    }

    private void OnBatteryDamageExamine(Entity<BatteryAmmoProviderComponent> ent, ref DamageExamineEvent args)
    {
        var proto = ProtoManager.Index<EntityPrototype>(ent.Comp.Prototype);
        DamageSpecifier? damageSpec = null;
        var damageType = string.Empty;

        if (proto.TryGetComponent<ProjectileComponent>(out var projectileComp, Factory))
        {
            if (!projectileComp.Damage.Empty)
            {
                damageType = Loc.GetString("damage-projectile");
                damageSpec = projectileComp.Damage * Damageable.UniversalProjectileDamageModifier;
            }
        }
        else if (proto.TryGetComponent<HitscanBasicDamageComponent>(out var hitscanComp, Factory))
        {
            if (!hitscanComp.Damage.Empty)
            {
                damageType = Loc.GetString("damage-hitscan");
                damageSpec = hitscanComp.Damage * Damageable.UniversalHitscanDamageModifier;
            }
        }
        if (damageSpec == null)
            return;

        _damageExamine.AddDamageExamine(args.Message, Damageable.ApplyUniversalAllModifiers(damageSpec), damageType);
    }

    private void OnBatteryTakeAmmo(Entity<BatteryAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        var shots = Math.Min(args.Shots, ent.Comp.Shots);

        if (shots == 0)
            return;

        for (var i = 0; i < shots; i++)
        {
            args.Ammo.Add(GetShootable(ent, args.Coordinates));
        }

        TakeCharge(ent, shots);
    }

    private void OnBatteryAmmoCount(Entity<BatteryAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        args.Count = ent.Comp.Shots;
        args.Capacity = ent.Comp.Capacity;
    }

    /// <summary>
    /// Use up the required amount of battery charge for firing.
    /// </summary>
    public void TakeCharge(Entity<BatteryAmmoProviderComponent> ent, int shots = 1)
    {
        // Take charge from either the BatteryComponent or PowerCellSlotComponent.
        var ev = new ChangeChargeEvent(-ent.Comp.FireCost * shots);
        RaiseLocalEvent(ent, ref ev);
        // UpdateShots is already called by the resulting ChargeChangedEvent
    }

    private (EntityUid? Entity, IShootable) GetShootable(BatteryAmmoProviderComponent component, EntityCoordinates coordinates)
    {

        var ent = Spawn(component.Prototype, coordinates);
        return (ent, EnsureShootable(ent));
    }

    public void UpdateShots(Entity<BatteryAmmoProviderComponent> ent)
    {
        var oldShots = ent.Comp.Shots;
        var oldCapacity = ent.Comp.Capacity;
        (var newShots, var newCapacity) = GetShots(ent);

        // Only dirty if necessary.
        if (oldShots == newShots && oldCapacity == newCapacity)
            return;

        ent.Comp.Shots = newShots;
        if (newCapacity > 0) // Don't make the capacity 0 when removing a power cell as this will make it be visualized as full instead of empty.
            ent.Comp.Capacity = newCapacity;

        // Update the ammo counter predictively if the change was predicted. On the server this does nothing.
        UpdateAmmoCount(ent.Owner);

        Dirty(ent); // Dirtying will update the client's ammo counter in an AfterAutoHandleStateEvent subscription in case it was not predicted.

        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        // Update the visuals.
        Appearance.SetData(ent.Owner, AmmoVisuals.HasAmmo, newShots != 0, appearance);
        Appearance.SetData(ent.Owner, AmmoVisuals.AmmoCount, newShots, appearance);
        if (newCapacity > 0) // Don't make the capacity 0 when removing a power cell as this will make it be visualized as full instead of empty.
            Appearance.SetData(ent.Owner, AmmoVisuals.AmmoMax, newCapacity, appearance);
    }

    // For server side changes the client's ammo counter needs to be updated as well.
    private void OnAfterAutoHandleState(Entity<BatteryAmmoProviderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateAmmoCount(ent); // Need to have prediction set to true because the state is applied repeatedly while prediction is running.
    }

    // For when a power cell gets inserted or removed.
    private void OnPowerCellChanged(Entity<BatteryAmmoProviderComponent> ent, ref PowerCellChangedEvent args)
    {
        UpdateShots(ent);
    }

    // If the entity is has a PowerCellSlotComponent then this event is relayed from the power cell to the slot entity.
    private void OnChargeChanged(Entity<BatteryAmmoProviderComponent> ent, ref ChargeChangedEvent args)
    {
        // Update the visuals and charge counter UI.
        UpdateShots(ent);
        // Queue the update for when the autorecharge reaches enough charge for another shot.
        UpdateNextUpdate(ent, args.CurrentCharge, args.MaxCharge, args.CurrentChargeRate);
    }

    private void UpdateNextUpdate(Entity<BatteryAmmoProviderComponent> ent, float currentCharge, float maxCharge, float currentChargeRate)
    {
        // Don't queue any updates if charge is constant.
        ent.Comp.NextUpdate = null;
        // ETA of the next full charge.
        if (currentChargeRate > 0f && currentCharge != maxCharge)
        {
            ent.Comp.NextUpdate = Timing.CurTime + TimeSpan.FromSeconds((ent.Comp.FireCost - (currentCharge % ent.Comp.FireCost)) / currentChargeRate);
            ent.Comp.ChargeTime = TimeSpan.FromSeconds(ent.Comp.FireCost / currentChargeRate);
        }
        else if (currentChargeRate < 0f && currentCharge != 0f)
        {
            ent.Comp.NextUpdate = Timing.CurTime + TimeSpan.FromSeconds(-(currentCharge % ent.Comp.FireCost) / currentChargeRate);
            ent.Comp.ChargeTime = TimeSpan.FromSeconds(-ent.Comp.FireCost / currentChargeRate);
        }
        Dirty(ent);
    }

    // Shots are only chached, not a DataField, so we need to refresh this when the game is loaded.
    private void OnBatteryStartup(Entity<BatteryAmmoProviderComponent> ent, ref ComponentStartup args)
    {
        if (_netManager.IsClient && !IsClientSide(ent.Owner))
            return; // Don't overwrite the server state in cases where the battery is not predicted.

        UpdateShots(ent);
    }

    /// <summary>
    /// Gets the current and maximum amount of shots from this entity's battery.
    /// This works for BatteryComponent and PowercellSlotComponent.
    /// </summary>
    public (int, int) GetShots(Entity<BatteryAmmoProviderComponent> ent)
    {
        var ev = new GetChargeEvent();
        RaiseLocalEvent(ent, ref ev);
        var currentShots = (int)(ev.CurrentCharge / ent.Comp.FireCost);
        var maxShots = (int)(ev.MaxCharge / ent.Comp.FireCost);

        return (currentShots, maxShots);
    }

    /// <summary>
    /// Update loop for refreshing the ammo counter for charging/draining batteries.
    /// </summary>
    private void UpdateBattery(float frameTime)
    {
        var curTime = Timing.CurTime;
        var hitscanQuery = EntityQueryEnumerator<BatteryAmmoProviderComponent>();
        while (hitscanQuery.MoveNext(out var uid, out var provider))
        {
            if (provider.NextUpdate == null || curTime < provider.NextUpdate)
                continue;
            UpdateShots((uid, provider));
            provider.NextUpdate += provider.ChargeTime; // Queue another update for when we reach the next full charge.
            Dirty(uid, provider);
            // TODO: Stop updating when full or empty.
        }
    }
}
