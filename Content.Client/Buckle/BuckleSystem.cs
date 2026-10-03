using Content.Client.Rotation;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Rotation;
using Robust.Client.GameObjects;

namespace Content.Client.Buckle;

internal sealed partial class BuckleSystem : SharedBuckleSystem
{
    [Dependency] private RotationVisualizerSystem _rotationVisualizerSystem = default!;

    #region Event Handlers

    [SubscribeLocalEvent]
    private void OnMobCollide(Entity<BuckleComponent> ent, ref AttemptMobCollideEvent args)
    {
        if (ent.Comp.Buckled)
        {
            args.Cancelled = true;
        }
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
