using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Projectiles;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// This system implements <see cref="QuickPickupComponent"/>'s behavior.
/// </summary>
public sealed partial class QuickPickupSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private EntityQuery<ItemComponent> _itemQuery;

    private const string DelayId = "quickPickup";

    public override void Initialize()
    {
        _itemQuery = GetEntityQuery<ItemComponent>();

        SubscribeLocalEvent<QuickPickupComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<QuickPickupComponent, AfterInteractEvent>(
            AfterInteract,
            before: [typeof(AreaPickupSystem)]
        );
    }

    private void OnMapInit(Entity<QuickPickupComponent> entity, ref MapInitEvent args)
    {
        _useDelay.SetLength(entity.Owner, entity.Comp.Cooldown, DelayId);
    }

    private void AfterInteract(Entity<QuickPickupComponent> pickupEntity, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            args.Target is not { Valid: true } target ||
            target == args.User ||
            !args.CanReach ||
            !_useDelay.TryResetDelay(pickupEntity, checkDelayed: true, id: DelayId) ||
            _container.IsEntityInContainer(target) ||
            !_itemQuery.HasComponent(target))
            return;

        // Copy `user` because the lambdas below cannot capture `ref args`.
        var user = args.User;
        Func<bool> tryInsert;
        if (TryComp<StorageComponent>(pickupEntity, out var storage))
        {
            tryInsert =
                () => _storage.PlayerInsertEntityInWorld((pickupEntity, storage), user, target);
        }
        else if (TryComp<BallisticAmmoProviderComponent>(pickupEntity, out var ammo))
        {
            tryInsert = () => _gun.TryBallisticInsert((pickupEntity, ammo), target, user);
        }
        else
        {
            DebugTools.Assert(
                $"Entity {pickupEntity} has {nameof(QuickPickupComponent)}, but neither {nameof(StorageComponent)} nor {nameof(BallisticAmmoProviderComponent)} to pickup into"
            );
            return;
        }

        if (TryComp(pickupEntity, out TransformComponent? pickupEntityXform) &&
            TryComp(target, out TransformComponent? targetXform))
        {
            _projectile.EmbedDetach(target, null, user);

            // Get the picked up entity's position _before_ inserting it, because that changes its position.
            var position = _transform.ToCoordinates(
                pickupEntityXform.ParentUid.IsValid() ? pickupEntityXform.ParentUid : pickupEntity.Owner,
                _transform.GetMapCoordinates(targetXform)
            );

            if (tryInsert())
            {
                EntityManager.RaiseSharedEvent(
                    new AnimateInsertingEntitiesEvent(
                        GetNetEntity(pickupEntity),
                        new List<NetEntity> { GetNetEntity(target) },
                        new List<NetCoordinates> { GetNetCoordinates(position) },
                        new List<Angle> { pickupEntityXform.LocalRotation }
                    ),
                    args.User
                );
            }

            args.Handled = true;
        }
    }
}
