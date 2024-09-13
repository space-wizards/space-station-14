using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.Interaction;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Buckle.Components;

/// <summary>
/// This component allows an entity to be buckled to an entity with a <see cref="StrapComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBuckleSystem))]
public sealed partial class BuckleComponent : Component
{
    /// <summary>
    /// The range from which this entity can buckle to a <see cref="StrapComponent"/>.
    /// Separated from normal interaction range to fix the "someone buckled to a strap
    /// across a table two tiles away" problem.
    /// </summary>
    [DataField]
    public float Range = SharedInteractionSystem.InteractionRange / 1.4f;

    /// <summary>
    /// True if the entity is buckled, false otherwise.
    /// </summary>
    [MemberNotNullWhen(true, nameof(BuckledTo))]
    public bool Buckled => BuckledTo != null;

    /// <summary>
    /// Whether or not collisions should be possible with the entity we are strapped to
    /// </summary>
    [DataField]
    public bool DontCollide;

    /// <summary>
    /// Whether or not we should be allowed to pull the entity we are strapped to
    /// </summary>
    [DataField]
    public bool PullStrap;

    /// <summary>
    /// The amount of time that must pass for this entity to
    /// be able to unbuckle after recently buckling.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    /// The time that this entity buckled at.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? BuckleTime;

    /// <summary>
    /// The strap that this component is buckled to.
    /// </summary>
    [DataField]
    public EntityUid? BuckledTo;

    /// <summary>
    /// The amount of space that this entity occupies in a
    /// <see cref="StrapComponent"/>.
    /// </summary>
    [DataField]
    public int Size = 100;

    /// <summary>
    /// Used for client rendering
    /// </summary>
    [ViewVariables] public int? OriginalDrawDepth;
}

[Serializable, NetSerializable]
public sealed class BuckleState(NetEntity? buckledTo, bool dontCollide, TimeSpan? buckleTime) : ComponentState
{
    public readonly NetEntity? BuckledTo = buckledTo;
    public readonly bool DontCollide = dontCollide;
    public readonly TimeSpan? BuckleTime = buckleTime;
}

public sealed partial class UnbuckleAlertEvent : BaseAlertEvent;

/// <summary>
/// Event raised directed at a strap entity before some entity gets buckled to it.
/// </summary>
[ByRefEvent]
public record struct StrapAttemptEvent(
    Entity<StrapComponent> Strap,
    Entity<BuckleComponent> Buckle,
    EntityUid? User,
    bool Popup)
{
    public bool Cancelled;
}

/// <summary>
/// Event raised directed at a buckle entity before it gets buckled to some strap entity.
/// </summary>
[ByRefEvent]
public record struct BuckleAttemptEvent(
    Entity<StrapComponent> Strap,
    Entity<BuckleComponent> Buckle,
    EntityUid? User,
    bool Popup)
{
    public bool Cancelled;
}

/// <summary>
/// Event raised directed at a strap entity before some entity gets unbuckled from it.
/// </summary>
[ByRefEvent]
public record struct UnstrapAttemptEvent(
    Entity<StrapComponent> Strap,
    Entity<BuckleComponent> Buckle,
    EntityUid? User,
    bool Popup)
{
    public bool Cancelled;
}

/// <summary>
/// Event raised directed at a buckle entity before it gets unbuckled.
/// </summary>
[ByRefEvent]
public record struct UnbuckleAttemptEvent(
    Entity<StrapComponent> Strap,
    Entity<BuckleComponent> Buckle,
    EntityUid? User,
    bool Popup)
{
    public bool Cancelled;
}

/// <summary>
/// Event raised directed at a strap entity after something has been buckled to it.
/// </summary>
[ByRefEvent]
public readonly record struct StrappedEvent(Entity<StrapComponent> Strap, Entity<BuckleComponent> Buckle);

/// <summary>
/// Event raised directed at a buckle entity after it has been buckled.
/// </summary>
[ByRefEvent]
public readonly record struct BuckledEvent(Entity<StrapComponent> Strap, Entity<BuckleComponent> Buckle);

/// <summary>
/// Event raised directed at a strap entity after something has been unbuckled from it.
/// </summary>
[ByRefEvent]
public readonly record struct UnstrappedEvent(Entity<StrapComponent> Strap, Entity<BuckleComponent> Buckle);

/// <summary>
/// Event raised directed at a buckle entity after it has been unbuckled from some strap entity.
/// </summary>
[ByRefEvent]
public readonly record struct UnbuckledEvent(Entity<StrapComponent> Strap, Entity<BuckleComponent> Buckle);

[Serializable, NetSerializable]
public enum BuckleVisuals
{
    Buckled
}
