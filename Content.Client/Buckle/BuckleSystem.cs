using System.Numerics;
using Content.Client.Rotation;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Rotation;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Buckle;

internal sealed class BuckleSystem : SharedBuckleSystem
{
    [Dependency] private readonly RotationVisualizerSystem _rotationVisualizerSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BuckleComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<StrapComponent, MoveEvent>(OnStrapMoveEvent);
        SubscribeLocalEvent<BuckleComponent, ComponentStartup>(OnBuckleStartup);
        SubscribeLocalEvent<BuckleComponent, BuckledEvent>(OnBuckledEvent);
        SubscribeLocalEvent<BuckleComponent, UnbuckledEvent>(OnUnbuckledEvent);
        SubscribeLocalEvent<BuckleComponent, AttemptMobCollideEvent>(OnMobCollide);
    }

    private void OnMobCollide(Entity<BuckleComponent> ent, ref AttemptMobCollideEvent args)
    {
        if (ent.Comp.Buckled)
        {
            args.Cancelled = true;
        }
    }

    private void OnStrapMoveEvent(EntityUid uid, StrapComponent component, ref MoveEvent args)
    {
        // I'm moving this to the client-side system, but for the sake of posterity let's keep this comment:
        // > This is mega cursed. Please somebody save me from Mr Buckle's wild ride

        // The nice thing is its still true, this is quite cursed, though maybe not omega cursed anymore.
        // This code is garbage, it doesn't work with rotated viewports. I need to finally get around to reworking
        // sprite rendering for entity layers & direction dependent sorting.

        // Future notes:
        // Right now this doesn't handle: other grids, other grids rotating, the camera rotation changing, and many other fun rotation specific things
        // The entire thing should be a concern of the engine, or something engine helps to implement properly.
        // Give some of the sprite rotations their own drawdepth, maybe as an offset within the rsi, or something like this
        // And we won't ever need to set the draw depth manually

        if (args.NewRotation == args.OldRotation)
            return;

        if (!TryComp<SpriteComponent>(uid, out var strapSprite))
            return;

        var angle = _xformSystem.GetWorldRotation(uid) + _eye.CurrentEye.Rotation; // Get true screen position, or close enough

        var isNorth = angle.GetCardinalDir() == Direction.North;
        foreach (var buckledEntity in component.BuckledEntities)
        {
            if (!TryComp<BuckleComponent>(buckledEntity, out var buckle))
                continue;

            if (!TryComp<SpriteComponent>(buckledEntity, out var buckledSprite))
                continue;

            if (isNorth)
            {
                // This will only assign if empty, it won't get overwritten by new depth on multiple calls, which do happen easily
                buckle.OriginalDrawDepth ??= buckledSprite.DrawDepth;
                _sprite.SetDrawDepth((buckledEntity, buckledSprite), strapSprite.DrawDepth - 1);
            }
            else if (buckle.OriginalDrawDepth.HasValue)
            {
                _sprite.SetDrawDepth((buckledEntity, buckledSprite), buckle.OriginalDrawDepth.Value);
                buckle.OriginalDrawDepth = null;
            }
        }
    }

    // need to support entities which are "created" buckled
    private void OnBuckleStartup(Entity<BuckleComponent> ent, ref ComponentStartup args)
    {
        if (!ent.Comp.Buckled)
            return;

        if (!TryComp<SpriteComponent>(ent, out var buckledSprite))
            return;

        var strap = ent.Comp.BuckledTo!;
        if (!TryComp<StrapComponent>(strap, out var strapComp))
            return;

        _sprite.SetOffset((ent.Owner, buckledSprite), buckledSprite.Offset + strapComp.BuckleOffset);

        if (strapComp.Overlay is { } overlay)
        {
            var idx = _sprite.LayerMapReserve((ent, buckledSprite), StrapVisualLayers.Overlay);
            _sprite.LayerSetSprite((ent, buckledSprite), idx, overlay);
            _sprite.LayerSetScale((ent.Owner, buckledSprite), idx, Vector2.One / buckledSprite.Scale); // reverse scalar values for things like dwarves
            UpdateBuckleOverlay((ent.Owner, ent, buckledSprite, Transform(ent)));
        }
        else
        {
            ent.Comp.OriginalDrawDepth ??= buckledSprite.DrawDepth;
            if (TryComp<SpriteComponent>(strap, out var strapSprite))
                _sprite.SetDrawDepth((ent.Owner, buckledSprite), strapSprite.DrawDepth + 1);
        }
    }

    /// <summary>
    /// Lower the draw depth of the buckled entity without needing for the strap entity to rotate/move.
    /// Only do so when the entity is facing screen-local north
    /// </summary>
    private void OnBuckledEvent(Entity<BuckleComponent> ent, ref BuckledEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var buckledSprite))
            return;

        if (_timing.IsFirstTimePredicted)
            _sprite.SetOffset((ent.Owner, buckledSprite), buckledSprite.Offset + args.Strap.Comp.BuckleOffset);

        if (args.Strap.Comp.Overlay is { } overlay)
        {
            var idx = _sprite.LayerMapReserve((ent, buckledSprite), StrapVisualLayers.Overlay);
            _sprite.LayerSetSprite((ent, buckledSprite), idx, overlay);
            _sprite.LayerSetScale((ent.Owner, buckledSprite), idx, Vector2.One / buckledSprite.Scale); // reverse scalar values for things like dwarves
            UpdateBuckleOverlay((ent.Owner, ent, buckledSprite, Transform(ent)));
        }
        else
        {
            ent.Comp.OriginalDrawDepth ??= buckledSprite.DrawDepth;
            if (TryComp<SpriteComponent>(args.Strap, out var strapSprite))
                _sprite.SetDrawDepth((ent.Owner, buckledSprite), strapSprite.DrawDepth + 1);
        }
    }

    /// <summary>
    /// Was the draw depth of the buckled entity lowered? Reset it upon unbuckling.
    /// </summary>
    private void OnUnbuckledEvent(Entity<BuckleComponent> ent, ref UnbuckledEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        if (_timing.IsFirstTimePredicted)
            _sprite.SetOffset((ent.Owner, sprite), sprite.Offset - args.Strap.Comp.BuckleOffset);

        if (args.Strap.Comp.Overlay is not null)
        {
            _sprite.RemoveLayer(ent.Owner, StrapVisualLayers.Overlay);
        }
        else
        {
            if (ent.Comp.OriginalDrawDepth != null)
                _sprite.SetDrawDepth((ent.Owner, sprite), ent.Comp.OriginalDrawDepth.Value);
            ent.Comp.OriginalDrawDepth = null;
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!TryComp<EyeComponent>(_playerManager.LocalEntity, out var eye))
            return;

        var query = EntityQueryEnumerator<BuckleComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var buckle, out var sprite, out var xform))
        {
            if (!_sprite.LayerMapTryGet((uid, sprite), StrapVisualLayers.Overlay, out var spriteLayer, false))
                continue;

            if (!buckle.Buckled)
            {
                _sprite.RemoveLayer((uid, sprite), spriteLayer);
                continue;
            }

            UpdateBuckleOverlay((uid, buckle, sprite, xform), eye.Zoom);
        }
    }

    private void UpdateBuckleOverlay(Entity<BuckleComponent, SpriteComponent, TransformComponent> ent, Vector2? zoom = null)
    {
        var (uid, buckle, sprite, xform) = ent;

        if (zoom == null)
        {
            if (!TryComp<EyeComponent>(_playerManager.LocalEntity, out var eye))
                return;
            zoom = eye.Zoom;
        }

        // we best be careful on the scary-ass client code
        if (!TryComp<TransformComponent>(buckle.BuckledTo, out var strapXform) ||
            !TryComp<StrapComponent>(buckle.BuckledTo, out var strapComp))
            return;

        var bucklePos = _eye.MapToScreen(_xformSystem.GetMapCoordinates(xform));
        var strapPos = _eye.MapToScreen(_xformSystem.GetMapCoordinates(strapXform));
        var dif = (strapPos.Position - bucklePos.Position) / EyeManager.PixelsPerMeter * zoom.Value - strapComp.BuckleOffset;

        _sprite.LayerSetOffset((uid, sprite), StrapVisualLayers.Overlay, dif);
    }

    private void OnAppearanceChange(EntityUid uid, BuckleComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<RotationVisualsComponent>(uid, out var rotVisuals))
            return;

        if (!Appearance.TryGetData<bool>(uid, BuckleVisuals.Buckled, out var buckled, args.Component) ||
            !buckled ||
            args.Sprite == null)
        {
            _rotationVisualizerSystem.SetHorizontalAngle((uid, rotVisuals), rotVisuals.DefaultRotation);
            return;
        }

        // Animate strapping yourself to something at a given angle
        // TODO: Dump this when buckle is better
        _rotationVisualizerSystem.AnimateSpriteRotation(uid, args.Sprite, rotVisuals.HorizontalRotation, 0.125f);
    }
}
