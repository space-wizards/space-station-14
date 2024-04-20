using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.Respiration.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class RespiratorComponent  : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    /// <summary>
    ///     The interval between updates. CycleTime (Inhale/exhale time) is added on top of this
    /// </summary>
    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan CycleRate = TimeSpan.FromSeconds(1);


}
