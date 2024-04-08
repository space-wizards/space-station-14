using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ReagentOnItem;

[RegisterComponent]
public sealed partial class SpaceGlueOnItemComponent : ReagentOnItemComponent
{
    [DataField("until", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Until;

    [DataField("duration", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration = TimeSpan.FromSeconds(20);
}