using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Projectiles;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// This system implements <see cref="AreaPickupComponent"/>'s behavior.
/// </summary>
public sealed partial class AreaPickupSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;


    private EntityQuery<ItemComponent> _itemQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private const string DelayId = "areaPickup";
    private const int FallbackItemWeight = 1;

    private static readonly AudioParams AudioParams = AudioParams.Default
        .WithMaxDistance(7f)
        .WithVolume(-2f);

    public override void Initialize()
    {
        _itemQuery = GetEntityQuery<ItemComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<AreaPickupComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AreaPickupComponent, AfterInteractEvent>(AfterInteract);
    }

    private void OnMapInit(Entity<AreaPickupComponent> entity, ref MapInitEvent args)
    {
        _useDelay.SetLength(entity.Owner, entity.Comp.Cooldown, DelayId);
    }

    private readonly HashSet<EntityUid> _entitiesInRange = [];

    private void AfterInteract(Entity<AreaPickupComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            !args.CanReach ||
            !_useDelay.TryResetDelay(entity, checkDelayed: true, id: DelayId))
            return;

        // Find entities near where the interaction click was.
        _entitiesInRange.Clear();
        _entityLookup.GetEntitiesInRange(
            args.ClickLocation,
            entity.Comp.Radius,
            _entitiesInRange,
            LookupFlags.Dynamic | LookupFlags.Sundries
        );

        // Filter out anything that definitely can't be picked up.
        var pickupCandidates = new List<Entity<ItemComponent>>();
        foreach (var entityInRange in _entitiesInRange)
        {
            if (entityInRange == args.User ||
                !_itemQuery.TryGetComponent(entityInRange, out var itemComp) ||
                !_prototype.HasIndex(itemComp.Size) ||
                !_interaction.InRangeUnobstructed(args.User, entityInRange))
                continue;
            pickupCandidates.Add((entityInRange, itemComp));
        }

        // No candidates to pick up.
        if (pickupCandidates.Count == 0)
            return;

        // Ask other systems to tell us what to pick up.
        var ev = new BeforeAreaPickupEvent(pickupCandidates, AreaPickupComponent.MaximumPickupLimit);
        RaiseLocalEvent(entity, ev);
        if (ev.EntitiesToPickUp.Count == 0)
        {
            // No systems told us to pick anything up.
            DebugTools.Assert(!ev.Handled, "Zero entities to pickup means this event should not be handled.");
            return;
        }

        DebugTools.Assert(ev.Handled, "Non-zero entities to pickup means this event should be handled.");

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ev.EntitiesToPickUp.Sum(GetWeight) * AreaPickupComponent.DelayPerItemWeight,
            new AreaPickupDoAfterEvent(GetNetEntityList(_entitiesInRange)),
            entity,
            target: entity)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    /// <summary>
    /// This function helps with handling <see cref="BeforeAreaPickupEvent"/> by enforcing
    /// <see cref="AreaPickupComponent.MaximumPickupLimit"/>. Callers need only pass in the event and
    /// <paramref name="canPickup">a predicate indicating whether a given candidate can be picked up</paramref>. Returns
    /// true if any items should be picked up and the event should be considered handled, false otherwise.
    /// </summary>
    public bool DoBeforeAreaPickup(ref BeforeAreaPickupEvent ev, Predicate<Entity<ItemComponent>> canPickup)
    {
        DebugTools.Assert(
            ev.EntitiesToPickUp.Count == 0,
            $"{nameof(BeforeAreaPickupEvent)} should not be raised with an empty {nameof(BeforeAreaPickupEvent.EntitiesToPickUp)}."
        );

        // Collect candidates which can be picked up, up to the maximum.
        foreach (var entityInRange in ev.PickupCandidates)
        {
            if (!canPickup(entityInRange))
                continue;

            ev.EntitiesToPickUp.Add(entityInRange);

            if (ev.EntitiesToPickUp.Count >= ev.MaxPickups)
                break;
        }

        return ev.EntitiesToPickUp.Count > 0;
    }

    private readonly List<EntityUid> _pickedUp = [];
    private readonly List<EntityCoordinates> _positions = [];
    private readonly List<Angle> _angles = [];

    /// <summary>
    /// This function helps with handling <see cref="AreaPickupDoAfterEvent"/> by handling
    /// <see cref="AnimateInsertingEntitiesEvent">animating picked up entities</see> and rechecking validity of picked
    /// up entities while also invoking <paramref name="tryPickup"/>. Returns true if either <paramref name="args"/>
    /// specifies to entities to pickup or if at least one entity was picked up and therefor the doafter should be
    /// considered handled; returns false otherwise.
    /// </summary>
    public bool TryDoAreaPickup(
        ref AreaPickupDoAfterEvent args,
        Entity<AreaPickupComponent?> entity,
        SoundSpecifier? pickupSound,
        Func<EntityUid, bool> tryPickup
    )
    {
        // Nothing to try to pick up, early return as handled.
        if (args.Entities.Count == 0)
            return true;

        if (!_xformQuery.TryGetComponent(entity, out var pickupEntityXform))
            return false;

        _pickedUp.Clear();
        _positions.Clear();
        _angles.Clear();

        // Collect position info for the items to pick up and try to actually pick them up.
        var entCount = Math.Min(AreaPickupComponent.MaximumPickupLimit, args.Entities.Count);
        foreach (var entityToPickUp in GetEntityList(args.Entities.ToList().GetRange(0, entCount)))
        {
            // Check again, situation may have changed for some entities, but we'll still pick up any that are valid
            if (entityToPickUp == args.Args.User ||
                _container.IsEntityInContainer(entityToPickUp) ||
                !_itemQuery.HasComponent(entityToPickUp) ||
                !_xformQuery.TryGetComponent(entityToPickUp, out var targetXform) ||
                targetXform.MapID != pickupEntityXform.MapID)
                continue;

            if (TryComp<EmbeddableProjectileComponent>(entityToPickUp, out var embeddable))
            {
                _projectile.EmbedDetach(entityToPickUp, embeddable, args.User);
            }

            // Get the picked up entity's position _before_ inserting it, because that changes its position.
            var position = _transform.ToCoordinates(
                pickupEntityXform.ParentUid.IsValid() ? pickupEntityXform.ParentUid : entity.Owner,
                _transform.GetMapCoordinates(targetXform)
            );

            // Actually insert the item.
            if (!tryPickup(entityToPickUp))
                continue;

            _pickedUp.Add(entityToPickUp);
            _positions.Add(position);
            _angles.Add(targetXform.LocalRotation);
        }

        // Nothing actually got picked up.
        if (_pickedUp.Count == 0)
            return false;

        // Play a sound and animate the items being picked up.
        _audio.PlayPredicted(pickupSound, entity, args.User, AudioParams);
        EntityManager.RaiseSharedEvent(
            new AnimateInsertingEntitiesEvent(
                GetNetEntity(entity),
                GetNetEntityList(_pickedUp),
                GetNetCoordinatesList(_positions),
                _angles
            ),
            args.User
        );
        return true;
    }

    private int GetWeight(Entity<ItemComponent> entity)
    {
        _prototype.Resolve(entity.Comp.Size, out var itemSize);
        return itemSize?.Weight ?? FallbackItemWeight;
    }
}
