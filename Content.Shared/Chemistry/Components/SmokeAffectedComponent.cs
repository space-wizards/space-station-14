using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// This is used for entities which are currently being affected by smoke.
/// Manages the gradual metabolism every second.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SmokeAffectedComponent : Component
{
    /// <summary>
    /// The time at which the next smoke metabolism will occur.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextSecond;

    /// <summary>
    /// The smoke that is currently affecting this entity.
    /// </summary>
    [DataField]
    public EntityUid SmokeEntity;
}
