using Content.Shared.Interaction;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Buckle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedBuckleSystem))]
public sealed partial class BuckleComponent : Component
{
    /// <summary>
    /// True if the entity is buckled, false otherwise.
    /// </summary>
    [AutoNetworkedField, DataField]
    public bool Buckled;

    /// <summary>
    /// The amount of time that must pass for this entity to
    /// be able to unbuckle after recently buckling.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    /// The amount of space that this entity occupies in a
    /// <see cref="StrapComponent"/>.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Size = 100;

    /// <summary>
    /// Used for client rendering
    /// </summary>
    [ViewVariables] public int? OriginalDrawDepth;
}

[ByRefEvent]
public record struct BuckleAttemptEvent(EntityUid StrapEntity, EntityUid BuckledEntity, EntityUid UserEntity, bool Buckling, bool Cancelled = false);

[ByRefEvent]
public readonly record struct BuckleChangeEvent(EntityUid StrapEntity, EntityUid BuckledEntity, bool Buckling);

[Serializable, NetSerializable]
public enum BuckleVisuals : byte
{
    Buckled
}
