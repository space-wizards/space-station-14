using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.ReagentOnItem;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Examine;

namespace Content.Server.ReagentOnItem;

public sealed class SpaceLubeOnItemSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpaceLubeOnItemComponent, ContainerGettingInsertedAttemptEvent>(OnHandPickUp);
        SubscribeLocalEvent<SpaceLubeOnItemComponent, ExaminedEvent>(OnExamine);
    }

    private void OnHandPickUp(EntityUid uid, SpaceLubeOnItemComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        _inventory.TryGetSlotEntity(args.Container.Owner, "gloves", out var gloves);

        // If whoever is picking it up has nonstick gloves then don't cause any issues!
        if (HasComp<NonStickSurfaceComponent>(gloves))
            return;

        // No more lube ;(
        if (component.AmountOfReagentLeft < 1)
        {
            RemComp<SpaceLubeOnItemComponent>(uid);
            return;
        }

        // Only reduce the amount of reagent on the item if you hit the proability.
        var randDouble = _random.NextDouble();
        if (randDouble > 1 - component.ChanceToDecreaseReagentOnGrab)
        {
            component.AmountOfReagentLeft--;
        }

        // Don't let them pick up the item.
        args.Cancel();

        // Throw the item away!
        var user = args.Container.Owner;
        _transform.SetCoordinates(uid, Transform(user).Coordinates);
        _transform.AttachToGridOrMap(uid);
        _throwing.TryThrow(uid, _random.NextVector2(), strength: component.PowerOfThrowOnPickup);
        _popup.PopupEntity(Loc.GetString("space-lube-on-item-slip", ("target", Identity.Entity(uid, EntityManager))), user, user, PopupType.MediumCaution);
    }

    // Show that the item is lubed if someone inspects it.
    private void OnExamine(EntityUid uid, SpaceLubeOnItemComponent comp, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            args.PushMarkup(Loc.GetString("space-lube-on-item-inspect"));
        }
    }
}
