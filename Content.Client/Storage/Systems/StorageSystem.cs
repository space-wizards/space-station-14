using System.Linq;
using System.Numerics;
using Content.Client.Animations;
using Content.Shared.Hands;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Storage.Events;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Storage.Systems;

public sealed class StorageSystem : SharedStorageSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly EntityPickupAnimationSystem _entityPickupAnimation = default!;
    [Dependency] private readonly AnimationPlayerSystem _animations = default!;

    private Dictionary<EntityUid, ItemStorageLocation> _oldStoredItems = new();

    private List<(StorageBoundUserInterface Bui, bool Value)> _queuedBuis = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StorageComponent, ComponentHandleState>(OnStorageHandleState);
        SubscribeNetworkEvent<PickupAnimationEvent>(HandlePickupAnimation);
        SubscribeNetworkEvent<StorageAnimationEvent>(HandleStorageAnimation);
        SubscribeAllEvent<AnimateInsertingEntitiesEvent>(HandleAnimatingInsertingEntities);
    }

    private void OnStorageHandleState(EntityUid uid, StorageComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StorageComponentState state)
            return;

        component.Grid.Clear();
        component.Grid.AddRange(state.Grid);
        component.MaxItemSize = state.MaxItemSize;
        component.Whitelist = state.Whitelist;
        component.Blacklist = state.Blacklist;
        component.StorageInsertSound = state.StorageInsertSound;
        component.StorageRemoveSound = state.StorageRemoveSound;
        component.StorageOpenSound = state.StorageOpenSound;
        component.StorageCloseSound = state.StorageCloseSound;
        component.DefaultStorageOrientation = state.DefaultStorageOrientation;

        _oldStoredItems.Clear();

        foreach (var item in component.StoredItems)
        {
            _oldStoredItems.Add(item.Key, item.Value);
        }

        component.StoredItems.Clear();

        foreach (var (nent, location) in state.StoredItems)
        {
            var ent = EnsureEntity<StorageComponent>(nent, uid);
            component.StoredItems[ent] = location;
        }

        component.SavedLocations.Clear();

        foreach (var loc in state.SavedLocations)
        {
            component.SavedLocations[loc.Key] = new(loc.Value);
        }

        UpdateOccupied((uid, component));

        var uiDirty = !component.StoredItems.SequenceEqual(_oldStoredItems);

        if (uiDirty && UI.TryGetOpenUi<StorageBoundUserInterface>(uid, StorageComponent.StorageUiKey.Key, out var storageBui))
        {
            storageBui.Refresh();
            // Make sure nesting still updated.
            var player = _player.LocalEntity;

            if (NestedStorage && player != null && ContainerSystem.TryGetContainingContainer((uid, null, null), out var container) &&
                UI.TryGetOpenUi<StorageBoundUserInterface>(container.Owner, StorageComponent.StorageUiKey.Key, out var containerBui))
            {
                _queuedBuis.Add((containerBui, false));
            }
        }
    }

    public override void UpdateUI(Entity<StorageComponent?> entity)
    {
        if (UI.TryGetOpenUi<StorageBoundUserInterface>(entity.Owner, StorageComponent.StorageUiKey.Key, out var sBui))
        {
            sBui.Refresh();
        }
    }

    protected override void HideStorageWindow(EntityUid uid, EntityUid actor)
    {
        if (UI.TryGetOpenUi<StorageBoundUserInterface>(uid, StorageComponent.StorageUiKey.Key, out var storageBui))
        {
            _queuedBuis.Add((storageBui, false));
        }
    }

    protected override void ShowStorageWindow(EntityUid uid, EntityUid actor)
    {
        if (UI.TryGetOpenUi<StorageBoundUserInterface>(uid, StorageComponent.StorageUiKey.Key, out var storageBui))
        {
            _queuedBuis.Add((storageBui, true));
        }
    }

    /// <inheritdoc />
    public override void PlayPickupAnimation(EntityUid uid, EntityCoordinates initialCoordinates, EntityCoordinates finalCoordinates,
        Angle initialRotation, EntityUid? user = null)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        PickupAnimation(uid, initialCoordinates, finalCoordinates, initialRotation);
    }

    /// <inheritdoc/>
    public override void PlayStorageAnimation(EntityUid uid, Vector2 scale, EntityUid? user = null)
    {
        if (!_timing.IsFirstTimePredicted || // Checks that this doesn't plays twice because of prediction.
            !TryComp<AnimationPlayerComponent>(uid, out var animations) || // Gets Animation player component.
            !TryComp<SpriteComponent>(uid, out var sprite) || // Gets sprite component.
            _animations.HasRunningAnimation(uid, "storage_animation_bounce")) // Checks that animation doesn't plays twice (that can cause very big problems).
            return;

        _animations.Play(new Entity<AnimationPlayerComponent>(uid, animations), new Animation
        {
            Length = TimeSpan.FromMilliseconds(400),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        // I suppose InQuad is the most fine solution here, its not really fast/slow or too much chaotic, but adds some noise.
                        // Biggest part of easing don't really different because animation is pretty short so I don't see really big issues with usage of just quad.
                        new AnimationTrackProperty.KeyFrame(sprite.Scale, 0, Easings.InQuad), // Start frame with start scale.
                        new AnimationTrackProperty.KeyFrame(sprite.Scale * scale, 0.1f, Easings.InQuad), // Here we decraise thickness and increaise height of sprite.
                        new AnimationTrackProperty.KeyFrame(sprite.Scale, 0.2f, Easings.InQuad), // Here we return start scale, but because of some sheningans its cursed (like a 1.23132131 height) so there is two additional keyframes.
                        new AnimationTrackProperty.KeyFrame(sprite.Scale, 0.3f, Easings.InQuad),
                        new AnimationTrackProperty.KeyFrame(sprite.Scale, 0.4f, Easings.InQuad),
                    }
                },
            }
        }, "storage_animation_bounce");
    }

    private void HandlePickupAnimation(PickupAnimationEvent msg)
    {
        PickupAnimation(GetEntity(msg.ItemUid), GetCoordinates(msg.InitialPosition), GetCoordinates(msg.FinalPosition), msg.InitialAngle);
    }

    public void PickupAnimation(EntityUid item, EntityCoordinates initialCoords, EntityCoordinates finalCoords, Angle initialAngle)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (TransformSystem.InRange(finalCoords, initialCoords, 0.1f) ||
            !Exists(initialCoords.EntityId) || !Exists(finalCoords.EntityId))
        {
            return;
        }

        var finalMapPos = TransformSystem.ToMapCoordinates(finalCoords).Position;
        var finalPos = Vector2.Transform(finalMapPos, TransformSystem.GetInvWorldMatrix(initialCoords.EntityId));

        _entityPickupAnimation.AnimateEntityPickup(item, initialCoords, finalPos, initialAngle);
    }

    public void HandleStorageAnimation(StorageAnimationEvent msg)
    {
        PlayStorageAnimation(GetEntity(msg.Uid), msg.Scale);
    }

    /// <summary>
    /// Animate the newly stored entities in <paramref name="msg"/> flying towards this storage's position
    /// </summary>
    /// <param name="msg"></param>
    public void HandleAnimatingInsertingEntities(AnimateInsertingEntitiesEvent msg)
    {
        TryComp(GetEntity(msg.Storage), out TransformComponent? transformComp);

        for (var i = 0; msg.StoredEntities.Count > i; i++)
        {
            var entity = GetEntity(msg.StoredEntities[i]);

            var initialPosition = msg.EntityPositions[i];
            if (Exists(entity) && transformComp != null)
            {
                _entityPickupAnimation.AnimateEntityPickup(entity, GetCoordinates(initialPosition), transformComp.LocalPosition, msg.EntityAngles[i]);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
        {
            return;
        }

        // This update loop exists just to synchronize with UISystem and avoid 1-tick delays.
        // If deferred opens / closes ever get removed you can dump this.
        foreach (var (bui, open) in _queuedBuis)
        {
            if (open)
            {
                bui.Show();
            }
            else
            {
                bui.Hide();
            }
        }

        _queuedBuis.Clear();
    }
}
