using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Components;

namespace Content.Shared.Clothing;

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
    }

    /// <summary>
    /// When item is unequipped from the shoe slot, friction, aceleration and collide on impact return to default settings.
    /// </summary>
    public void OnGotUnequipped(EntityUid uid, SkatesComponent component, ClothingGotUnequippedEvent args)
    {
        if (!TryComp(args.Wearer, out MovementSpeedModifierComponent? speedModifier))
            return;

        _move.ChangeFriction(args.Wearer, MovementSpeedModifierComponent.DefaultFriction, MovementSpeedModifierComponent.DefaultFrictionNoInput, MovementSpeedModifierComponent.DefaultAcceleration, speedModifier);
        _impact.ChangeCollide(args.Wearer, component.DefaultMinimumSpeed, component.DefaultStunSeconds, component.DefaultDamageCooldown, component.DefaultSpeedDamage);
    }

    /// <summary>
    /// When item is equipped into the shoe slot, friction, acceleration and collide on impact are adjusted.
    /// </summary>
    private void OnGotEquipped(EntityUid uid, SkatesComponent component, ClothingGotEquippedEvent args)
    {
        _move.ChangeFriction(args.Wearer, component.Friction, component.FrictionNoInput, component.Acceleration);
        _impact.ChangeCollide(args.Wearer, component.MinimumSpeed, component.StunSeconds, component.DamageCooldown, component.SpeedDamage);
    }
}
