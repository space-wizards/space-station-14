using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class WaddleClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaddleWhenWornComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<WaddleWhenWornComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid entity, WaddleWhenWornComponent comp, ClothingGotEquippedEvent args)
    {
        var waddleAnimComp = EnsureComp<WaddleAnimationComponent>(args.Wearer);

        waddleAnimComp.AnimationLength = comp.AnimationLength;
        waddleAnimComp.HopIntensity = comp.HopIntensity;
        waddleAnimComp.RunAnimationLengthMultiplier = comp.RunAnimationLengthMultiplier;
        waddleAnimComp.TumbleIntensity = comp.TumbleIntensity;
    }

    private void OnGotUnequipped(EntityUid entity, WaddleWhenWornComponent comp, ClothingGotUnequippedEvent args)
    {
        RemComp<WaddleAnimationComponent>(args.Wearer);
    }
}
