using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Glue;

[RegisterComponent]
public sealed class GluedComponent : Component
{
    /// <summary>
    /// Reverts name to before prefix event (essentially removes prefix).
    /// </summary>
    [DataField("beforeGluedEntityName"), ViewVariables(VVAccess.ReadOnly)]
    public string BeforeGluedEntityName = string.Empty;

    [DataField("until", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Until = TimeSpan.Zero;

    [DataField("duration", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration = TimeSpan.Zero;
}
