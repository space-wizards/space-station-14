using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Inventory;
using Content.Shared.Tag;

namespace Content.Shared.Eye;

public sealed class VisionDarkenerSystem : EntitySystem
{
    [Dependency] private readonly SharedDarkenedVisionSystem _darkenedVision = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisionDarkenerComponent, GetVisionDarkeningEvent>(OnGetVisionDarkening);
        SubscribeLocalEvent<VisionDarkenerComponent, InventoryRelayedEvent<GetVisionDarkeningEvent>>(OnGetVisionDarkening);

        SubscribeLocalEvent<VisionDarkenerComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<VisionDarkenerComponent, ClothingGotUnequippedEvent>(OnGotUnquipped);
    }

    private void OnGetVisionDarkening(Entity<VisionDarkenerComponent> ent, ref GetVisionDarkeningEvent args)
    {
        args.Strength += ent.Comp.Strength;
    }

    private void OnGetVisionDarkening(Entity<VisionDarkenerComponent> ent, ref InventoryRelayedEvent<GetVisionDarkeningEvent> args)
    {
        args.Args.Strength += ent.Comp.Strength;
    }

    private void OnGotEquipped(Entity<VisionDarkenerComponent> ent, ref ClothingGotEquippedEvent args)
    {
        _darkenedVision.UpdateVisionDarkening(args.Wearer);
    }

    private void OnGotUnquipped(Entity<VisionDarkenerComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        _darkenedVision.UpdateVisionDarkening(args.Wearer);
    }
}
