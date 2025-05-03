using System.Numerics;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Examine;
using Robust.Shared.GameStates;
using Content.Shared.Hands;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ReagentOnItem;

public sealed class SpaceLubeOnItemSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpaceLubeOnItemComponent, MapInitEvent>(MapInit);
        SubscribeLocalEvent<SpaceLubeOnItemComponent, GotAttemptedHandPickupEvent>(OnHandPickUp);
        SubscribeLocalEvent<SpaceLubeOnItemComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SpaceLubeOnItemComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
    }

    private void MapInit(EntityUid uid, SpaceLubeOnItemComponent component, MapInitEvent args)
    {
        _nameMod.RefreshNameModifiers(uid);
    }

    private void OnHandPickUp(EntityUid uid, SpaceLubeOnItemComponent component, GotAttemptedHandPickupEvent args)
    {
        if (args.Cancelled)
            return;

        var handContainer = args.Hand.Container;
        if (handContainer == null)
            return;

        var entityWhoPickedUp = handContainer.Owner;

        _inventory.TryGetSlotEntity(entityWhoPickedUp, "gloves", out var gloves);

        if (HasComp<NonStickSurfaceComponent>(gloves))
            return;

        args.Cancel();

        // I tried so hard but this has got to be server side only trust me
        if (!_net.IsServer)
            return;

        if (component.LastTimeAttemptedPickup + component.PickupCooldown > _timing.CurTime)
            return;

        component.LastTimeAttemptedPickup = _timing.CurTime;

        // This is so if throwing ever gets predicted it won't lag (Or any of this gets predicted...).
        var rand = new System.Random((int)_timing.CurTick.Value);

        var randDouble = rand.NextDouble();
        if (randDouble > 1 - component.ChanceToDecreaseReagentOnGrab)
        {
            component.EffectStacks -= 1;
        }

        if (_container.TryRemoveFromContainer(uid, true, out var wasInContainer) || !wasInContainer)
        {
            var worldPos = _xform.GetWorldPosition(Transform(entityWhoPickedUp));
            var itemPosition = _xform.GetWorldPosition(Transform(uid));
            var vecFromObjectToEnt = worldPos - itemPosition;

            Vector2 throwDirection;
            if (vecFromObjectToEnt == Vector2.Zero)
            {
                // Just throw it a random direction!
                throwDirection = new Vector2(rand.NextFloat(-1,1), rand.NextFloat(-1,1));
            }
            else
            {
                // This calculates the spread of the crowbar so it won't go in a straight line to the entity picking it up!
                var randNegPosOne = 2 * rand.NextDouble() - 1;
                var spread = Math.PI / 5;
                var rotation = new Angle(spread * randNegPosOne);
                throwDirection = rotation.RotateVec(vecFromObjectToEnt);
            }

            _throwing.TryThrow(uid, throwDirection.Normalized(), baseThrowSpeed: component.PowerOfThrowOnPickup);
            _popup.PopupEntity(Loc.GetString("space-lube-on-item-slip", ("target", Identity.Entity(uid, EntityManager))), entityWhoPickedUp, entityWhoPickedUp, PopupType.MediumCaution);
        }

        if (component.EffectStacks < 1)
        {
            RemComp<SpaceLubeOnItemComponent>(uid);
            _nameMod.RefreshNameModifiers(uid);
        }
    }

    private void OnExamine(EntityUid uid, SpaceLubeOnItemComponent comp, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString("space-lube-on-item-inspect"));
    }

    private void OnRefreshNameModifiers(Entity<SpaceLubeOnItemComponent> entity, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("lubed-name-prefix");
    }
}
