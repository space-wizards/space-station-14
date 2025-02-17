using Content.Shared.DeadSpace.NightVision;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server.DeadSpace.NightVision;

public sealed class NightVisionClothingSystem : EntitySystem
{
    [Dependency] private readonly NightVisionSystem _nightVision = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<NightVisionClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<NightVisionClothingComponent, InventoryRelayedEvent<CanNightVisionAttemptEvent>>(OnNightVisionTrySee);
    }

    private void OnNightVisionTrySee(Entity<NightVisionClothingComponent> nVClothing, ref InventoryRelayedEvent<CanNightVisionAttemptEvent> args)
    {
        args.Args.Cancel();
    }

    private void OnGotEquipped(EntityUid entity, NightVisionClothingComponent comp, ref GotEquippedEvent args)
    {
        var nightVisionComp = EnsureComp<NightVisionComponent>(args.Equipee);

        nightVisionComp.Color = comp.Color;

        _nightVision.UpdateIsNightVision(args.Equipee);
    }

    private void OnGotUnequipped(EntityUid entity, NightVisionClothingComponent comp, ref GotUnequippedEvent args)
    {
        if (!TryComp<NightVisionComponent>(args.Equipee, out var nightVisionComp))
            return;

        nightVisionComp.Color = comp.Color;

        _nightVision.UpdateIsNightVision(args.Equipee); // Actually we should remove NightVisionComponent, and if you know how to properly do it, please fix this
    }
}
