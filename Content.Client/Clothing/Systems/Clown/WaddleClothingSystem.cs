using Content.Shared.Clothing.Components.Clown;
using Content.Shared.Clown;
using Content.Shared.Inventory.Events;

namespace Content.Client.Clothing.Systems.Clown;

public sealed class WaddleClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaddleComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<WaddleComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid entity, WaddleComponent comp, GotEquippedEvent args)
    {
        EnsureComp<WaddleAnimationComponent>(args.Equipee);
    }

    private void OnGotUnequipped(EntityUid entity, WaddleComponent comp, GotUnequippedEvent args)
    {
        RemComp<WaddleAnimationComponent>(args.Equipee);
    }
}
