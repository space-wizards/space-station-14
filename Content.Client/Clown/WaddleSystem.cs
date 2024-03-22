using Content.Shared.Clown;
using Content.Shared.Inventory.Events;

namespace Content.Client.Clown;

public sealed class WaddleSystem : EntitySystem
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
