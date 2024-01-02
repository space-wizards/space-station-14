using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// Used for marking regular revs as well as storing icon prototypes so you can see fellow revs.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed partial class RevolutionaryComponent : Component
{
    /// <summary>
    /// The status icon prototype displayed for revolutionaries
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StatusIconPrototype> RevStatusIcon = "RevolutionaryFaction";

    /// <summary>
    /// The time at which the rev will try and escape their flashed state
    /// </summary>
    [DataField("flashStartEscapeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextFlashEscapeTime = TimeSpan.Zero;

    /// <summary>
    /// How many times has the rev attempted to escape? This is set to zero if they're re-flashed.
    /// </summary>
    public int EscapeAttemptsSoFar;
}

[Serializable, NetSerializable]
public sealed class FreedFromControlMessage : EntityEventArgs
{
}
