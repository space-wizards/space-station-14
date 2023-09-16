using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;

namespace Content.Server.Clothing;

public sealed class SkatesSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkatesComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SkatesComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotUnequipped(EntityUid uid, SkatesComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "shoes")
        {
            if (HasComp<SkaterComponent>(args.Equipee))
                RemComp<SkaterComponent>(args.Equipee);
        }
    }

    private void OnGotEquipped(EntityUid uid, SkatesComponent component, GotEquippedEvent args)
    {
        if (args.Slot == "shoes")
        {
            if (!HasComp<SkaterComponent>(args.Equipee))
                AddComp<SkaterComponent>(args.Equipee);
        }
    }
}
