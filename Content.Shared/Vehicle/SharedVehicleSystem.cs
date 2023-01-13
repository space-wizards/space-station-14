using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Vehicle.Components;
using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics.Pull;
using Robust.Shared.Serialization;
using Robust.Shared.Containers;
using Content.Shared.Tag;
using Content.Shared.Audio;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;

namespace Content.Shared.Vehicle;

/// <summary>
/// Stores the VehicleVisuals and shared event
/// Nothing for a system but these need to be put somewhere in
/// Content.Shared
/// </summary>
public abstract partial class SharedVehicleSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _modifier = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;

    private const string KeySlot = "key_slot";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InVehicleComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<RiderComponent, PullAttemptEvent>(OnRiderPull);
        SubscribeLocalEvent<VehicleComponent, RefreshMovementSpeedModifiersEvent>(OnVehicleModifier);
        SubscribeLocalEvent<VehicleComponent, ComponentStartup>(OnVehicleStartup);
        SubscribeLocalEvent<VehicleComponent, MoveEvent>(OnVehicleRotate);
        SubscribeLocalEvent<VehicleComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<VehicleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<VehicleComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);
    }

    /// <summary>
    /// Handle adding keys to the ignition, give stuff the InVehicleComponent so it can't be picked
    /// up by people not in the vehicle.
    /// </summary>
    private void OnEntInserted(EntityUid uid, VehicleComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != KeySlot ||
            !_tagSystem.HasTag(args.Entity, "VehicleKey")) return;

        // Enable vehicle
        var inVehicle = EnsureComp<InVehicleComponent>(args.Entity);
        inVehicle.Vehicle = component;

        component.HasKey = true;

        // Audiovisual feedback
        _ambientSound.SetAmbience(uid, true);
        _tagSystem.AddTag(uid, "DoorBumpOpener");
        _modifier.RefreshMovementSpeedModifiers(uid);
    }

    /// <summary>
    /// Turn off the engine when key is removed.
    /// </summary>
    private void OnEntRemoved(EntityUid uid, VehicleComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != KeySlot || !RemComp<InVehicleComponent>(args.Entity))
            return;

        // Disable vehicle
        component.HasKey = false;
        _ambientSound.SetAmbience(uid, false);
        _tagSystem.RemoveTag(uid, "DoorBumpOpener");
        _modifier.RefreshMovementSpeedModifiers(uid);
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
    private void OnVehicleRotate(EntityUid uid, VehicleComponent component, ref MoveEvent args)
    {
        if (args.NewRotation == args.OldRotation)
            return;

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
        if (TryComp<StrapComponent>(uid, out var strap))
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
        if (!TryComp<StrapComponent>(component.Owner, out var strap))
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

    private void OnGetAdditionalAccess(EntityUid uid, VehicleComponent component, ref GetAdditionalAccessEvent args)
    {
        if (component.Rider == null)
            return;
        var rider = component.Rider.Value;

        args.Entities.Add(rider);
        _access.FindAccessItemsInventory(rider, out var items);
        args.Entities = args.Entities.Union(items).ToHashSet();
    }

    /// <summary>
    /// Set the draw depth for the sprite.
    /// </summary>
    protected void UpdateDrawDepth(EntityUid uid, int drawDepth)
    {
        Appearance.SetData(uid, VehicleVisuals.DrawDepth, drawDepth);
    }

    /// <summary>
    /// Set whether the vehicle's base layer is animating or not.
    /// </summary>
    protected void UpdateAutoAnimate(EntityUid uid, bool autoAnimate)
    {
        Appearance.SetData(uid, VehicleVisuals.AutoAnimate, autoAnimate);
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

