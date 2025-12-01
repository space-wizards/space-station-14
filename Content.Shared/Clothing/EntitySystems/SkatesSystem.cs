using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Clothing.Components;

namespace Content.Shared.Clothing.EntitySystems;

/// <summary>
/// Changes the friction and acceleration of the wearer and also the damage on impact variables of thew wearer when hitting a static object.
/// </summary>
public sealed class SkatesSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _move = default!;
    [Dependency] private readonly DamageOnHighSpeedImpactSystem _impact = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkatesComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SkatesComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<SkatesComponent, InventoryRelayedEvent<RefreshFrictionModifiersEvent>>(OnRefreshFrictionModifiers);
    }

    /// <summary>
    /// When item is unequipped from the shoe slot, friction, aceleration and collide on impact return to default settings.
    /// </summary>
    private void OnGotUnequipped(Entity<SkatesComponent> entity, ref ClothingGotUnequippedEvent args)
    {
        _move.RefreshFrictionModifiers(args.Wearer);
        _impact.ChangeCollide(args.Wearer, entity.Comp.DefaultMinimumSpeed, entity.Comp.DefaultStunSeconds, entity.Comp.DefaultDamageCooldown, entity.Comp.DefaultSpeedDamage);
    }

    /// <summary>
    /// When item is equipped into the shoe slot, friction, acceleration and collide on impact are adjusted.
    /// </summary>
    private void OnGotEquipped(Entity<SkatesComponent> entity, ref ClothingGotEquippedEvent args)
    {
        _move.RefreshFrictionModifiers(args.Wearer);
        _impact.ChangeCollide(args.Wearer, entity.Comp.MinimumSpeed, entity.Comp.StunSeconds, entity.Comp.DamageCooldown, entity.Comp.SpeedDamage);
    }

    private void OnRefreshFrictionModifiers(Entity<SkatesComponent> ent,
        ref InventoryRelayedEvent<RefreshFrictionModifiersEvent> args)
    {
        args.Args.ModifyFriction(ent.Comp.Friction, ent.Comp.FrictionNoInput);
        args.Args.ModifyAcceleration(ent.Comp.Acceleration);
    }
}
