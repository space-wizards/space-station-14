using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ReagentOnItem;

[RegisterComponent]
public sealed partial class SpaceGlueOnItemComponent : ReagentOnItemComponent
{
    [DataField("timeOfNextCheck", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeOfNextCheck;

    [DataField("durationPerUnit"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DurationPerUnit = TimeSpan.FromSeconds(6);
}