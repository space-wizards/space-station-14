using Content.Client.Rotation;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Buckle
{
    internal sealed class BuckleSystem : SharedBuckleSystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly RotationVisualizerSystem _rotationVisualizerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BuckleComponent, ComponentHandleState>(OnBuckleHandleState);
            SubscribeLocalEvent<BuckleComponent, AppearanceChangeEvent>(OnAppearanceChange);
        }

        private void OnBuckleHandleState(EntityUid uid, BuckleComponent buckle, ref ComponentHandleState args)
        {
            if (args.Current is not BuckleComponentState state)
                return;

            buckle.Buckled = state.Buckled;
            buckle.LastEntityBuckledTo = state.LastEntityBuckledTo;
            buckle.DontCollide = state.DontCollide;

            _actionBlocker.UpdateCanMove(uid);

            if (!TryComp(uid, out SpriteComponent? ownerSprite))
                return;

            if (HasComp<VehicleComponent>(buckle.LastEntityBuckledTo))
                return;

            // Adjust draw depth when the chair faces north so that the seat back is drawn over the player.
            // Reset the draw depth when rotated in any other direction.
            // TODO when ECSing, make this a visualizer
            // This code was written before rotatable viewports were introduced, so hard-coding Direction.North
            // and comparing it against LocalRotation now breaks this in other rotations. This is a FIXME, but
            // better to get it working for most people before we look at a more permanent solution.
            if (buckle.Buckled &&
                buckle.LastEntityBuckledTo != null &&
                Transform(buckle.LastEntityBuckledTo.Value).LocalRotation.GetCardinalDir() == Direction.North &&
                TryComp<SpriteComponent>(buckle.LastEntityBuckledTo, out var buckledSprite))
            {
                buckle.OriginalDrawDepth ??= ownerSprite.DrawDepth;
                ownerSprite.DrawDepth = buckledSprite.DrawDepth - 1;
                return;
            }

            // If here, we're not turning north and should restore the saved draw depth.
            if (buckle.OriginalDrawDepth.HasValue)
            {
                ownerSprite.DrawDepth = buckle.OriginalDrawDepth.Value;
                buckle.OriginalDrawDepth = null;
            }
        }

        private void OnAppearanceChange(EntityUid uid, BuckleComponent component, ref AppearanceChangeEvent args)
        {
            if (!TryComp<RotationVisualsComponent>(uid, out var rotVisuals))
                return;

            if (!_appearanceSystem.TryGetData<int>(uid, StrapVisuals.RotationAngle, out var angle, args.Component) ||
                !_appearanceSystem.TryGetData<bool>(uid, BuckleVisuals.Buckled, out var buckled, args.Component) ||
                !buckled ||
                args.Sprite == null)
            {
                _rotationVisualizerSystem.SetHorizontalAngle(uid, RotationVisualsComponent.DefaultRotation, rotVisuals);
                return;
            }

            // Animate strapping yourself to something at a given angle
            _rotationVisualizerSystem.SetHorizontalAngle(uid, Angle.FromDegrees(angle), rotVisuals);
            // TODO: Dump this when buckle is better
            _rotationVisualizerSystem.AnimateSpriteRotation(uid, args.Sprite, rotVisuals.HorizontalRotation, 0.125f);
        }
    }
}
