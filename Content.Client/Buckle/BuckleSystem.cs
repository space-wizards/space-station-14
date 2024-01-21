using Content.Client.Rotation;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Rotation;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Buckle;

internal sealed class BuckleSystem : SharedBuckleSystem
{
    [Dependency] private readonly RotationVisualizerSystem _rotationVisualizerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BuckleComponent, AfterAutoHandleStateEvent>(OnBuckleAfterAutoHandleState);
        SubscribeLocalEvent<BuckleComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnBuckleAfterAutoHandleState(EntityUid uid, BuckleComponent component, ref AfterAutoHandleStateEvent args)
    {
        ActionBlocker.UpdateCanMove(uid);

        if (!TryComp<SpriteComponent>(uid, out var ownerSprite))
            return;

        if (HasComp<VehicleComponent>(component.LastEntityBuckledTo))
            return;

        // Adjust draw depth when the chair faces north so that the seat back is drawn over the player.
        // Reset the draw depth when rotated in any other direction.
        // TODO when ECSing, make this a visualizer
        // This code was written before rotatable viewports were introduced, so hard-coding Direction.North
        // and comparing it against LocalRotation now breaks this in other rotations. This is a FIXME, but
        // better to get it working for most people before we look at a more permanent solution.
        if (component is { Buckled: true, LastEntityBuckledTo: { } } &&
            Transform(component.LastEntityBuckledTo.Value).LocalRotation.GetCardinalDir() == Direction.North &&
            TryComp<SpriteComponent>(component.LastEntityBuckledTo, out var buckledSprite))
        {
            component.OriginalDrawDepth ??= ownerSprite.DrawDepth;
            ownerSprite.DrawDepth = buckledSprite.DrawDepth - 1;
            return;
        }

        // If here, we're not turning north and should restore the saved draw depth.
        if (component.OriginalDrawDepth.HasValue)
        {
            ownerSprite.DrawDepth = component.OriginalDrawDepth.Value;
            component.OriginalDrawDepth = null;
        }
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
