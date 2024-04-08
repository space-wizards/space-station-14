using Content.Shared.IdentityManagement;
using Content.Shared.Lube;
using Content.Shared.Popups;
using Content.Shared.ReagentOnItem;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;

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
        SubscribeLocalEvent<SpaceLubeOnItemComponent, ComponentInit>(OnInit);

    }

    private void OnInit(EntityUid uid, SpaceLubeOnItemComponent component, ComponentInit args)
    {
        Log.Log(LogLevel.Debug, "Here 3333");
        Log.Log(LogLevel.Debug, component.AmountOfReagentLeft.ToString());
    }

    private void OnHandPickUp(EntityUid uid, SpaceLubeOnItemComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        _inventory.TryGetSlotEntity(args.Container.Owner, "gloves", out var gloves);

        if (HasComp<NonStickSurfaceComponent>(gloves))
        {
            return;
        }

        if (component.AmountOfReagentLeft < 1)
        {
            RemComp<SpaceLubeOnItemComponent>(uid);
            return;
        }

        var randDouble = _random.NextDouble();
        if (randDouble > 1 - component.ChanceToDecreaseReagentOnGrab)
        {
            component.AmountOfReagentLeft--;
        }
        args.Cancel();
        var user = args.Container.Owner;
        _transform.SetCoordinates(uid, Transform(user).Coordinates);
        _transform.AttachToGridOrMap(uid);
        _throwing.TryThrow(uid, _random.NextVector2(), strength: component.PowerOfThrowOnPickup);
        _popup.PopupEntity(Loc.GetString("lube-slip", ("target", Identity.Entity(uid, EntityManager))), user, user, PopupType.MediumCaution);
    }
}
