using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Server.Damage.Components;

namespace Content.Server.Clothing;

public sealed class SkatesSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _move = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkatesComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SkatesComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    public void OnGotUnequipped(EntityUid uid, SkatesComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "shoes")
        {
            if (TryComp<MovementSpeedModifierComponent>(args.Equipee, out var mover))
            {
                mover.Friction = 20f;
                mover.FrictionNoInput = null;
                mover.MinimumFrictionSpeed = 0.005f;
                _move.RefreshMovementSpeedModifiers(args.Equipee);
            }

            if (TryComp<DamageOnHighSpeedImpactComponent>(args.Equipee, out var impact))
            {
                impact.MinimumSpeed = 20f;
            }
        }
    }

    private void OnGotEquipped(EntityUid uid, SkatesComponent component, GotEquippedEvent args)
    {
        if (args.Slot == "shoes")
        {
            if (TryComp<MovementSpeedModifierComponent>(args.Equipee, out var mover))
            {
                mover.Friction = 5f;
                mover.FrictionNoInput = 5f;
                _move.RefreshMovementSpeedModifiers(args.Equipee);
            }

            if (TryComp<DamageOnHighSpeedImpactComponent>(args.Equipee, out var impact))
            {
                impact.MinimumSpeed = 4f;
            }
        }
    }
}
