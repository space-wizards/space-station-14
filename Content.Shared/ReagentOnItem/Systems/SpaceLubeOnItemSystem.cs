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

namespace Content.Shared.ReagentOnItem;

public sealed class SpaceLubeOnItemSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpaceLubeOnItemComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpaceLubeOnItemComponent, GotAttemptedHandPickupEvent>(OnHandPickUp);
        SubscribeLocalEvent<SpaceLubeOnItemComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SpaceLubeOnItemComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);

        SubscribeLocalEvent<SpaceLubeOnItemComponent, ComponentGetState>(GetSpaceLubeState);
        SubscribeLocalEvent<SpaceLubeOnItemComponent, ComponentHandleState>(HandleSpaceLubeState);
    }

    private void OnInit(EntityUid uid, SpaceLubeOnItemComponent component, ComponentInit args)
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

        if (component.EffectStacks < 1)
        {
            RemComp<SpaceLubeOnItemComponent>(uid);
            _nameMod.RefreshNameModifiers(uid);
            return;
        }

        args.Cancel();

        var randDouble = _random.NextDouble();
        if (randDouble > 1 - component.ChanceToDecreaseReagentOnGrab)
        {
            component.EffectStacks -= 1;
        }

        Dirty(uid, component);

        var entTransform = Transform(entityWhoPickedUp);
        var xformQuery = GetEntityQuery<TransformComponent>();
        var worldPos = _xform.GetWorldPosition(entTransform, xformQuery);
        var vecFromObjectToEnt = worldPos - _xform.GetWorldPosition(uid, xformQuery);

        // This calculates the spread of the crowbar so it wont go in a straight line
        // to the entity picking it up!
        var randNegPosOne = 2 * _random.NextDouble() - 1;
        var spread = Math.PI / 5;
        var rotation = new Angle(spread * randNegPosOne);

        _throwing.TryThrow(uid, rotation.RotateVec(vecFromObjectToEnt.Normalized()), strength: component.PowerOfThrowOnPickup);
        _popup.PopupPredicted(Loc.GetString("space-lube-on-item-slip", ("target", Identity.Entity(uid, EntityManager))), entityWhoPickedUp, entityWhoPickedUp, PopupType.MediumCaution);
    }

    private void OnExamine(EntityUid uid, SpaceLubeOnItemComponent comp, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            args.PushMarkup(Loc.GetString("space-lube-on-item-inspect"));
        }
    }

    private void OnRefreshNameModifiers(Entity<SpaceLubeOnItemComponent> entity, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("lubed-name-prefix");
    }

    private void HandleSpaceLubeState(EntityUid uid, SpaceLubeOnItemComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ReagentOnItemComponentState state)
            return;

        component.EffectStacks = state.EffectStacks;
        component.MaxStacks = state.MaxStacks;
    }

    private void GetSpaceLubeState(EntityUid uid, SpaceLubeOnItemComponent component, ref ComponentGetState args)
    {
        args.State = new ReagentOnItemComponentState(component.EffectStacks, component.MaxStacks);
    }
}
