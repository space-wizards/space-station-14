using Content.Client.Rotation;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Rotation;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Buckle;

internal sealed partial class BuckleSystem : SharedBuckleSystem
{
    [Dependency] private RotationVisualizerSystem _rotationVisualizerSystem = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private SharedTransformSystem _xformSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    #region Event Handlers

    [SubscribeLocalEvent]
    private void OnStrapMoveEvent(Entity<StrapComponent> ent, ref MoveEvent args)
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

        if (!ent.Comp.ModifyBuckleDrawDepth)
            return;

        if (args.NewRotation == args.OldRotation)
            return;

        if (!_spriteQuery.TryComp(ent, out SpriteComponent? strapSprite))
            return;

        var newAngle = args.NewRotation + _eye.CurrentEye.Rotation;
        var oldAngle = args.OldRotation + _eye.CurrentEye.Rotation;

        if (newAngle.GetCardinalDir() == oldAngle.GetCardinalDir())
            return;

        var isNorth = newAngle.GetCardinalDir() == Direction.North;

        foreach (var buckledEntity in ent.Comp.BuckledEntities)
        {
            if (!TryComp<BuckleComponent>(buckledEntity, out var buckle))
                continue;

            if (!_spriteQuery.TryComp(buckledEntity, out SpriteComponent? buckledSprite))
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

    [SubscribeLocalEvent]
    private void OnMobCollide(Entity<BuckleComponent> ent, ref AttemptMobCollideEvent args)
    {
        if (ent.Comp.Buckled)
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    /// Lower the draw depth of the buckled entity without needing for the strap entity to rotate/move.
    /// Only do so when the entity is facing screen-local north
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBuckledEvent(Entity<BuckleComponent> ent, ref BuckledEvent args)
    {
        if (!args.Strap.Comp.ModifyBuckleDrawDepth)
            return;

        if (!_spriteQuery.TryComp(args.Strap, out SpriteComponent? strapSprite))
            return;

        if (!_spriteQuery.TryComp(ent.Owner, out SpriteComponent? buckledSprite))
            return;

        var angle = _xformSystem.GetWorldRotation(args.Strap) + _eye.CurrentEye.Rotation; // Get true screen position, or close enough

        if (angle.GetCardinalDir() != Direction.North)
            return;

        ent.Comp.OriginalDrawDepth ??= buckledSprite.DrawDepth;
        _sprite.SetDrawDepth((ent.Owner, buckledSprite), strapSprite.DrawDepth - 1);
    }

    /// <summary>
    /// Was the draw depth of the buckled entity lowered? Reset it upon unbuckling.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUnbuckledEvent(Entity<BuckleComponent> ent, ref UnbuckledEvent args)
    {
        if (!args.Strap.Comp.ModifyBuckleDrawDepth)
            return;

        if (!_spriteQuery.TryComp(ent.Owner, out SpriteComponent? buckledSprite))
            return;

        if (!ent.Comp.OriginalDrawDepth.HasValue)
            return;

        _sprite.SetDrawDepth((ent.Owner, buckledSprite), ent.Comp.OriginalDrawDepth.Value);
        ent.Comp.OriginalDrawDepth = null;
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<BuckleComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!TryComp<RotationVisualsComponent>(ent, out var rotVisuals))
            return;

        if (!Appearance.TryGetData<bool>(ent, BuckleVisuals.Buckled, out var buckled, args.Component) ||
            !buckled ||
            args.Sprite == null)
        {
            _rotationVisualizerSystem.SetHorizontalAngle((ent, rotVisuals), rotVisuals.DefaultRotation);
            return;
        }

        // Animate strapping yourself to something at a given angle
        // TODO: Dump this when buckle is better
        _rotationVisualizerSystem.AnimateSpriteRotation(ent, args.Sprite, rotVisuals.HorizontalRotation, 0.125f);
    }
    #endregion Event Handlers
}
