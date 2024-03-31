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

        SubscribeLocalEvent<SkatesComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SkatesComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    /// <summary>
    /// When item is unequipped from the shoe slot, friction, aceleration and collide on impact return to default settings.
    /// </summary>
    public void OnGotUnequipped(EntityUid uid, SkatesComponent component, GotUnequippedEvent args)
    {
        if (!TryComp(args.Equipee, out MovementSpeedModifierComponent? speedModifier))
            return;

        if (args.Slot == "shoes")
        {
            _move.ChangeFriction(args.Equipee, MovementSpeedModifierComponent.DefaultFriction, MovementSpeedModifierComponent.DefaultFrictionNoInput, MovementSpeedModifierComponent.DefaultAcceleration, speedModifier);
            _impact.ChangeCollide(args.Equipee, component.DefaultMinimumSpeed, component.DefaultStunSeconds, component.DefaultDamageCooldown, component.DefaultSpeedDamage);
        }
    }

    /// <summary>
    /// When item is equipped into the shoe slot, friction, acceleration and collide on impact are adjusted.
    /// </summary>
    private void OnGotEquipped(EntityUid uid, SkatesComponent component, GotEquippedEvent args)
    {
        if (args.Slot == "shoes")
        { 
            _move.ChangeFriction(args.Equipee, component.Friction, component.FrictionNoInput, component.Acceleration);
            _impact.ChangeCollide(args.Equipee, component.MinimumSpeed, component.StunSeconds, component.DamageCooldown, component.SpeedDamage);
        }
    }
}
