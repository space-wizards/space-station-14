using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Server.Damage.Systems;

namespace Content.Server.Clothing;

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

    public void OnGotUnequipped(EntityUid uid, SkatesComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "shoes")
        {
            _move.ChangeFriction(args.Equipee, 20f, null, 20f);
            _impact.ChangeCollide(args.Equipee, 20f, 1f, 2f);
        }
    }

    private void OnGotEquipped(EntityUid uid, SkatesComponent component, GotEquippedEvent args)
    {
        if (args.Slot == "shoes")
        {
            _move.ChangeFriction(args.Equipee, 5f, 5f, 20f);
            _impact.ChangeCollide(args.Equipee, 4f, 1f, 2f);
        }
    }
}
