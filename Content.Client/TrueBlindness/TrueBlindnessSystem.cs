using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.TrueBlindness;
using Robust.Client.Audio.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.TrueBlindness;

public sealed class TrueBlindnessSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private const string GhostDefaultPrototype = "BlindnessGhost";
    private const string SoundGhostDefaultPrototype = "BlindnessSoundGhost";
    private const string GhostShaderPrototype = "Greyscale";
    private const float DefaultVisibleRange = 1.5f;

    private ShaderInstance? _shader;

    private EntityUid? _playerGhost;

    private TrueBlindnessOverlay _overlay = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AudioComponent, AudioEvents.AudioPlayEntityEvent>(AudioPlayedEntity);
        SubscribeLocalEvent<AudioComponent, AudioEvents.AudioPlayCoordinatesEvent>(AudioPlayedCoordinates);

        _shader = _proto.Index<ShaderPrototype>(GhostShaderPrototype).Instance();

        _overlay = new();
        _overlayMan.AddOverlay(_overlay);
    }

    public void AudioPlayedEntity(Entity<AudioComponent> uid, ref AudioEvents.AudioPlayEntityEvent args)
    {
        Log.Debug("how noisey");
        // if (!HasComp(_playerManager.LocalEntity, typeof(TrueBlindnessComponent)))
        //     return;
        CreateSoundGhost(args.Entity, uid, out _);

    }

    public void AudioPlayedCoordinates(Entity<AudioComponent> uid, ref AudioEvents.AudioPlayCoordinatesEvent args)
    {
        Log.Debug("how loud");
        // if (!HasComp(_playerManager.LocalEntity, typeof(TrueBlindnessComponent)))
        //     return;
        CreateSoundGhost(args.Coordinates.EntityId, uid, out _);

    }

    public bool VisibleFromPlayer(EntityUid playerUid, EntityUid objectUid)
    {
        var playerTransform = Transform(playerUid);
        var playerPosition = _transform.GetWorldPosition(playerTransform);
        var objectPosition = _transform.GetWorldPosition(objectUid);
        var offset = objectPosition - playerPosition;
        var direction = Vector2.Normalize(offset);
        if (!MathHelper.CloseToPercent(direction.LengthSquared(), 1))
            return false;
        var r = new CollisionRay(_transform.GetWorldPosition(playerTransform), direction, (int)CollisionGroup.FlyingMobMask);
        var cr = _physics.IntersectRay(playerTransform.MapID, r, maxLength:offset.Length(), ignoredEnt:playerUid, returnOnFirstHit:true).FirstOrNull();
        var rTrue = cr is null
                    || cr.Value.HitEntity == objectUid
                    || (TryComp(objectUid, out TrueBlindnessGhostComponent? ghostComp) &&
                        cr.Value.HitEntity == GetEntity(ghostComp.From));
        return rTrue;
    }


    public bool CreateSoundGhost(
        EntityUid from,
        Entity<AudioComponent> sound,
        [NotNullWhen(true)] out EntityUid? ghostEntity)
    {
        ghostEntity = null;
        var ghost = Spawn(SoundGhostDefaultPrototype);
        _transform.SetParent(ghost, _transform.GetParentUid(sound));
        _transform.SetWorldPosition(ghost, _transform.GetWorldPosition(from));
        EnsureComp(ghost, out TrueBlindnessGhostComponent ghostComponent);
        EnsureComp(ghost, out SpriteComponent ghostSprite);
        ghostComponent.From = null;
        ghostComponent.WasAnchored = false;
        ghostComponent.VisibleTime = ghostComponent.FadeoutTime = TimeSpan.FromMilliseconds(1000);
        ghostComponent.CreationTime = _timing.CurTime;
        ghostComponent.DeletionEligible = _timing.CurTime + ghostComponent.VisibleTime;
        ghostSprite.DrawDepth += (int)DrawDepth.Overlays - (int)DrawDepth.LowFloors + 1;
        // if (_playerManager.LocalEntity is not null)
        //     ghostSprite.CopyFrom(Comp<SpriteComponent>(_playerManager.LocalEntity.Value));
        ghostEntity = ghost;
        return true;
    }

    public bool CreateGhost(Entity<TrueBlindnessVisibleComponent> uid,
        [NotNullWhen(true)] out EntityUid? ghostEntity,
        bool applyShader = true,
        bool applyPosition = true)
    {
        ghostEntity = null;
        if (!TryComp(uid, out SpriteComponent? sprite))
            return false;
        var ghost = Spawn(GhostDefaultPrototype);
        EnsureComp(ghost, out SpriteComponent ghostSprite);
        EnsureComp(ghost, out TrueBlindnessGhostComponent ghostComponent);
        var xform = Transform(uid);
        _transform.SetParent(ghost, _transform.GetParentUid(uid));
        if (applyPosition)
        {
            _transform.SetWorldPositionRotation(ghost,
                _transform.GetWorldPosition(uid),
                _transform.GetWorldRotation(uid));
        }
        // TODO: Add SetWorldPositionRotation(EntityUid, (Vector2, Angle), TransformComponent?)
        // for use with GetWorldPositionRotation into SharedTransformSystem
        ghostComponent.From = GetNetEntity(uid);
        ghostComponent.WasAnchored = xform.Anchored;
        ghostComponent.VisibleTime = uid.Comp.VisibleTime;
        ghostComponent.FadeoutTime = uid.Comp.FadeoutTime;
        ghostComponent.CreationTime = _timing.CurTime;
        ghostComponent.DeletionEligible = _timing.CurTime + uid.Comp.BufferTime;
        ghostSprite.CopyFrom(sprite);
        if (applyShader)
            ghostSprite.PostShader = _shader;
        ghostSprite.DrawDepth += (int)DrawDepth.Overlays - (int)DrawDepth.LowFloors + 1; // Lol.
        ghostEntity = ghost;
        return true;
    }

    public void DeleteGhost(EntityUid ghost)
    {
        QueueDel(ghost);
    }

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _playerManager.LocalEntity;

        if (player is null || !HasComp(player, typeof(TrueBlindnessComponent)))
        {
            if (_playerGhost is not null)
            {
                QueueDel(_playerGhost);
                _playerGhost = null;
            }

            if (_overlay.Enabled)
            {
                _overlay.SetEnabled(false);

                var deletionQuery = EntityQueryEnumerator<TrueBlindnessGhostComponent>();

                while (deletionQuery.MoveNext(out var uid, out _))
                {
                    DeleteGhost(uid);
                }
            }
            return;
        }

        _overlay.SetEnabled(true);

        if (_playerGhost is null && TryComp(player, out TrueBlindnessVisibleComponent? playerVisible))
        {
            if (CreateGhost((player.Value, playerVisible), out _playerGhost, false, false))
            {
                _transform.SetParent(_playerGhost.Value, player.Value);
            }
        }

        if (_playerGhost is not null
            && TryComp(_playerGhost, out SpriteComponent? playerGhostSprite)
            && TryComp(player, out SpriteComponent? playerSprite))
            playerGhostSprite.CopyFrom(playerSprite);


        var visibleRange = DefaultVisibleRange;

        foreach (var entityUid in _inventory.GetHandOrInventoryEntities(player.Value, SlotFlags.PREVENTEQUIP))
        {
            if (TryComp(entityUid, out TrueBlindnessRangeExtendComponent? rangeExtend) &&
                rangeExtend.Range > visibleRange)
                visibleRange = rangeExtend.Range;
        }

        foreach (var uid in _lookup.GetEntitiesInRange(player.Value, visibleRange / 2, LookupFlags.Uncontained))
        {
            if (HasComp(uid, typeof(TrueBlindnessGhostComponent)))
            {
                if (VisibleFromPlayer(player.Value, uid))
                {
                    DeleteGhost(uid);
                }
                continue;
            }

            EnsureComp(uid, out TrueBlindnessVisibleComponent visible);
            if (_timing.CurTime < visible.LastGhost + visible.BufferTime)
                continue;
            if (!VisibleFromPlayer(player.Value, uid))
                continue;
            CreateGhost((uid, visible), out _);
        }


        var query = EntityQueryEnumerator<TrueBlindnessGhostComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var lifeSpan = _timing.CurTime - comp.CreationTime;

            if (comp.WasAnchored || _timing.CurTime < comp.DeletionEligible)
                continue;

            if (lifeSpan > comp.VisibleTime)
                DeleteGhost(uid);

            var fade = (float)(1 - (lifeSpan - comp.VisibleTime + comp.FadeoutTime) /
                comp.FadeoutTime);

            if (fade > 1 || fade < 0)
                continue;

            if (!TryComp(uid, out SpriteComponent? sprite))
                continue;

            sprite.Color = new Color(1f / fade, 1f / fade, 1f / fade, fade * fade);
        }

    }
}
