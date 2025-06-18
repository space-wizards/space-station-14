using System.Linq;
using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
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
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;


    private EntityQuery<ItemComponent> _itemQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private const string DelayId = "areaPickup";

    private static readonly AudioParams AudioParams = AudioParams.Default
        .WithMaxDistance(7f)
        .WithVolume(-2f);

    public override void Initialize()
    {
        _itemQuery = GetEntityQuery<ItemComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<AreaPickupComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AreaPickupComponent, AfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<StorageComponent, AreaPickupDoAfterEvent>(OnDoAfterStorage);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, AreaPickupDoAfterEvent>(OnDoAfterBallistic);
    }

    private void OnMapInit(Entity<AreaPickupComponent> entity, ref MapInitEvent args)
    {
        _useDelay.SetLength(entity.Owner, entity.Comp.Cooldown, DelayId);
    }

    private readonly List<EntityUid> _entitiesToPickUp = [];
    private readonly HashSet<EntityUid> _entitiesInRange = [];

    private void AfterInteract(Entity<AreaPickupComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            !args.CanReach ||
            !_useDelay.TryResetDelay(entity, checkDelayed: true, id: DelayId))
            return;

        Predicate<Entity<ItemComponent>> canInsert;
        if (TryComp<StorageComponent>(entity, out var storage))
        {
            canInsert = insertedEntity => _storage.CanInsert(
                entity,
                insertedEntity,
                out _,
                storage,
                insertedEntity.Comp
            );
        }
        else if (TryComp<BallisticAmmoProviderComponent>(entity, out var ammo))
        {
            canInsert = insertedEntity => _gun.CanInsertBallistic((entity, ammo), insertedEntity);
        }
        else
        {
            DebugTools.Assert(
                $"Entity {entity} has {nameof(AreaPickupComponent)}, but neither {nameof(StorageComponent)} nor {nameof(BallisticAmmoProviderComponent)} to pickup into"
            );
            return;
        }

        _entitiesToPickUp.Clear();
        _entitiesInRange.Clear();
        _entityLookup.GetEntitiesInRange(
            args.ClickLocation,
            entity.Comp.Radius,
            _entitiesInRange,
            LookupFlags.Dynamic | LookupFlags.Sundries
        );
        var delay = TimeSpan.Zero;

        foreach (var entityInRange in _entitiesInRange)
        {
            if (entityInRange == args.User ||
                // Need comp to get item size to get weight
                !_itemQuery.TryGetComponent(entityInRange, out var itemComp) ||
                !_prototype.TryIndex(itemComp.Size, out var itemSize) ||
                !canInsert((entityInRange, itemComp)) ||
                !_interaction.InRangeUnobstructed(args.User, entityInRange))
                continue;

            _entitiesToPickUp.Add(entityInRange);
            delay += itemSize.Weight * AreaPickupComponent.DelayPerItemWeight;

            if (_entitiesToPickUp.Count >= AreaPickupComponent.MaximumPickupLimit)
                break;
        }

        if (_entitiesToPickUp.Count >= 1)
        {
            var doAfterArgs = new DoAfterArgs(EntityManager,
                args.User,
                delay,
                new AreaPickupDoAfterEvent(GetNetEntityList(_entitiesToPickUp)),
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
    }

    private void OnDoAfterStorage(Entity<StorageComponent> entity, ref AreaPickupDoAfterEvent args)
    {
        var user = args.User;
        OnDoAfterGeneric(
            entity,
            ref args,
            entity.Comp.StorageInsertSound,
            entity.Comp.SilentStorageUserTag,
            entityToInsert =>
                _storage.PlayerInsertEntityInWorld(entity.AsNullable(), user, entityToInsert, playSound: false)
        );
    }

    private void OnDoAfterBallistic(Entity<BallisticAmmoProviderComponent> entity, ref AreaPickupDoAfterEvent args)
    {
        var user = args.User;
        OnDoAfterGeneric(
            entity,
            ref args,
            entity.Comp.SoundInsert,
            entity.Comp.SilentInsertUserTag,
            entityToInsert => _gun.TryBallisticInsert(entity, entityToInsert, user, suppressInsertionSound: true)
        );
    }

    private void OnDoAfterGeneric(
        EntityUid pickupEntity,
        ref AreaPickupDoAfterEvent args,
        SoundSpecifier? sound,
        ProtoId<TagPrototype>? silentStorageUserTag,
        Func<EntityUid, bool> tryInsert
    )
    {
        if (args.Handled ||
            args.Cancelled ||
            !HasComp<AreaPickupComponent>(pickupEntity) ||
            !_xformQuery.TryGetComponent(pickupEntity, out var pickupEntityXform))
            return;

        var successfullyInserted = new List<EntityUid>();
        var successfullyInsertedPositions = new List<EntityCoordinates>();
        var successfullyInsertedAngles = new List<Angle>();

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
                pickupEntityXform.ParentUid.IsValid() ? pickupEntityXform.ParentUid : pickupEntity,
                _transform.GetMapCoordinates(targetXform)
            );

            // Actually insert the item.
            if (!tryInsert(entityToPickUp))
                continue;

            successfullyInserted.Add(entityToPickUp);
            successfullyInsertedPositions.Add(position);
            successfullyInsertedAngles.Add(targetXform.LocalRotation);
        }

        // If we picked up at least one thing, play a sound and do a cool animation!
        if (successfullyInserted.Count > 0)
        {
            if (silentStorageUserTag is not { } tag ||
                !_tag.HasTag(args.User, tag))
            {
                _audio.PlayPredicted(sound, pickupEntity, args.User, AudioParams);
            }

            EntityManager.RaiseSharedEvent(
                new AnimateInsertingEntitiesEvent(
                    GetNetEntity(pickupEntity),
                    GetNetEntityList(successfullyInserted),
                    GetNetCoordinatesList(successfullyInsertedPositions),
                    successfullyInsertedAngles
                ),
                args.User
            );
        }

        args.Handled = true;
    }
}
