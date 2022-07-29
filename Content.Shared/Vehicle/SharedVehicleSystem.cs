using Content.Shared.Vehicle.Components;
using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics.Pull;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Vehicle;

/// <summary>
/// Stores the VehicleVisuals and shared event
/// Nothing for a system but these need to be put somewhere in
/// Content.Shared
/// </summary>
public abstract partial class SharedVehicleSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _modifier = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InVehicleComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<RiderComponent, PullAttemptEvent>(OnRiderPull);
        SubscribeLocalEvent<VehicleComponent, RefreshMovementSpeedModifiersEvent>(OnVehicleModifier);
        SubscribeLocalEvent<VehicleComponent, ComponentStartup>(OnVehicleStartup);
        SubscribeLocalEvent<VehicleComponent, RotateEvent>(OnVehicleRotate);
    }

    private void OnVehicleModifier(EntityUid uid, VehicleComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.HasKey)
        {
            args.ModifySpeed(0f, 0f);
        }
    }

    private void OnPickupAttempt(EntityUid uid, InVehicleComponent component, GettingPickedUpAttemptEvent args)
    {
        if (component.Vehicle == null || component.Vehicle.Rider != null && component.Vehicle.Rider != args.User)
            args.Cancel();
    }

    // TODO: Shitcode, needs to use sprites instead of actual offsets.
    private void OnVehicleRotate(EntityUid uid, VehicleComponent component, ref RotateEvent args)
    {
        // This first check is just for safety
        if (!HasComp<InputMoverComponent>(uid))
        {
            UpdateAutoAnimate(uid, false);
            return;
        }

        UpdateBuckleOffset(args.Component, component);
        UpdateDrawDepth(uid, GetDrawDepth(args.Component, component.NorthOnly));
    }

    private void OnVehicleStartup(EntityUid uid, VehicleComponent component, ComponentStartup args)
    {
        UpdateDrawDepth(uid, 2);

        // This code should be purged anyway but with that being said this doesn't handle components being changed.
        if (TryComp<SharedStrapComponent>(uid, out var strap))
        {
            component.BaseBuckleOffset = strap.BuckleOffset;
            strap.BuckleOffsetUnclamped = Vector2.Zero;
        }

        _modifier.RefreshMovementSpeedModifiers(uid);
    }

    /// <summary>
    /// Depending on which direction the vehicle is facing,
    /// change its draw depth. Vehicles can choose between special drawdetph
    /// when facing north or south. East and west are easy.
    /// </summary>
    protected int GetDrawDepth(TransformComponent xform, bool northOnly)
    {
        // TODO: I can't even
        if (northOnly)
        {
            return xform.LocalRotation.Degrees switch
            {
                < 135f => (int) DrawDepth.DrawDepth.Doors,
                <= 225f => (int) DrawDepth.DrawDepth.WallMountedItems,
                _ => 5
            };
        }
        return xform.LocalRotation.Degrees switch
        {
            < 45f =>  (int) DrawDepth.DrawDepth.Doors,
            <= 315f =>  (int) DrawDepth.DrawDepth.WallMountedItems,
            _ =>  (int) DrawDepth.DrawDepth.Doors,
        };
    }

    /// <summary>
    /// Change the buckle offset based on what direction the vehicle is facing and
    /// teleport any buckled entities to it. This is the most crucial part of making
    /// buckled vehicles work.
    /// </summary>
    protected void UpdateBuckleOffset(TransformComponent xform, VehicleComponent component)
    {
        if (!TryComp<SharedStrapComponent>(component.Owner, out var strap))
            return;

        // TODO: Strap should handle this but buckle E/C moment.
        var oldOffset = strap.BuckleOffsetUnclamped;

        strap.BuckleOffsetUnclamped = xform.LocalRotation.Degrees switch
        {
            < 45f => (0, component.SouthOverride),
            <= 135f => component.BaseBuckleOffset,
            < 225f  => (0, component.NorthOverride),
            <= 315f => (component.BaseBuckleOffset.X * -1, component.BaseBuckleOffset.Y),
            _ => (0, component.SouthOverride)
        };

        if (!oldOffset.Equals(strap.BuckleOffsetUnclamped))
            Dirty(strap);

        foreach (var buckledEntity in strap.BuckledEntities)
        {
            var buckleXform = Transform(buckledEntity);
            _transform.SetLocalPositionNoLerp(buckleXform, strap.BuckleOffset);
        }
    }

    /// <summary>
    /// Set the draw depth for the sprite.
    /// </summary>
    protected void UpdateDrawDepth(EntityUid uid, int drawDepth)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        appearance.SetData(VehicleVisuals.DrawDepth, drawDepth);
    }

    /// <summary>
    /// Set whether the vehicle's base layer is animating or not.
    /// </summary>
    protected void UpdateAutoAnimate(EntityUid uid, bool autoAnimate)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        appearance.SetData(VehicleVisuals.AutoAnimate, autoAnimate);
    }
}

/// <summary>
/// Stores the vehicle's draw depth mostly
/// </summary>
[Serializable, NetSerializable]
public enum VehicleVisuals : byte
{
    /// <summary>
    /// What layer the vehicle should draw on (assumed integer)
    /// </summary>
    DrawDepth,
    /// <summary>
    /// Whether the wheels should be turning
    /// </summary>
    AutoAnimate
}
/// <summary>
/// Raised when someone honks a vehicle horn
/// </summary>
public sealed class HonkActionEvent : InstantActionEvent { }

