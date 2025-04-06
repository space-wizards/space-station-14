using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Security.Components;

/// <summary>
/// This is used for storing information about a Genpop ID in order to correctly display it on examine.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class GenpopIdCardComponent : Component
{
    /// <summary>
    /// The crime committed, as a string.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Crime = "[Redacted]";

    /// <summary>
    /// The time at which the sentence started, used to calculate the sentence duration.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan StartTime;
}
