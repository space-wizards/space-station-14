using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.TrueBlindness;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Client.Player;
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
    private const string GhostShaderPrototype = "Greyscale";
    private const float DefaultVisibleRange = 1.5f;

    private ShaderInstance? _shader;


    private TrueBlindnessOverlay _overlay = default!;


    public override void Initialize()
    {
        base.Initialize();

        _shader = _proto.Index<ShaderPrototype>(GhostShaderPrototype).Instance();

        _overlay = new();
        _overlayMan.AddOverlay(_overlay);
    }

    public bool VisibleFromPlayer(EntityUid playerUid, EntityUid objectUid)
    {
        var playerTransform = Transform(playerUid);
        var playerPosition = _transform.GetWorldPosition(playerTransform);
        var objectPosition = _transform.GetWorldPosition(objectUid);
        var direction = Vector2.Normalize(objectPosition - playerPosition);
        if (!MathHelper.CloseToPercent(direction.LengthSquared(), 1))
            return false;
        var r = new CollisionRay(_transform.GetWorldPosition(playerTransform), direction, (int)CollisionGroup.FlyingMobMask);
        var cr = _physics.IntersectRay(playerTransform.MapID, r, maxLength:1.5f, ignoredEnt:playerUid, returnOnFirstHit:true).FirstOrNull();
        return cr is null
               || cr.Value.HitEntity == objectUid
               || (TryComp(objectUid, out TrueBlindnessGhostComponent? ghostComp) &&
                   GetEntity(ghostComp.From) == objectUid);
    }

    public bool CreateGhost(Entity<TrueBlindnessVisibleComponent> uid,
        [NotNullWhen(true)] out EntityUid? ghostEntity, bool applyShader = true)
    {
        ghostEntity = null;
        if (!TryComp(uid, out SpriteComponent? sprite))
            return false;
        var ghost = Spawn(GhostDefaultPrototype);
        EnsureComp(ghost, out SpriteComponent ghostSprite);
        EnsureComp(ghost, out TrueBlindnessGhostComponent ghostComponent);
        var xform = Transform(uid);
        _transform.SetParent(ghost, _transform.GetParentUid(uid));
        _transform.SetWorldPositionRotation(ghost,
            _transform.GetWorldPosition(uid),
            _transform.GetWorldRotation(uid));
        // TODO: Add SetWorldPositionRotation(EntityUid, (Vector2, Angle), TransformComponent?)
        // for use with GetWorldPositionRotation into SharedTransformSystem
        ghostComponent.From = GetNetEntity(uid);
        ghostComponent.WasAnchored = xform.Anchored;
        ghostComponent.VisibleTime = uid.Comp.VisibleTime;
        ghostComponent.FadeoutTime = uid.Comp.FadeoutTime;
        ghostComponent.CreationTime = _timing.CurTime;
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

        // if (TryComp(ghost, out SpriteComponent? sprite))
        //     sprite.PostShader?.Dispose();
    }

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _playerManager.LocalEntity;

        if (player is null || !HasComp(player, typeof(TrueBlindnessComponent)))
        {
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
                    DeleteGhost(uid);
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
            if (comp.WasAnchored || _timing.CurTime < comp.DeletionEligible)
                continue;

            if (_timing.CurTime - comp.CreationTime > comp.VisibleTime)
                DeleteGhost(uid);

            var fade = (float)(1 - (_timing.CurTime - comp.CreationTime - comp.VisibleTime + comp.FadeoutTime) /
                comp.FadeoutTime);

            if (fade > 1 || fade < 0)
                continue;

            if (!TryComp(uid, out SpriteComponent? sprite))
                continue;

            // if (sprite.PostShader is null || sprite.PostShader.Disposed)
            //     continue;
            //
            // sprite.PostShader.SetParameter("Alpha", (float)fade);

            sprite.Color = new Color(1f, 1f, 1f, fade);
        }

    }
}
