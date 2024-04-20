using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ReagentOnItem;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SpaceGlueOnItemComponent : ReagentOnItemComponent
{
    [DataField, AutoPausedField]
    public TimeSpan TimeOfNextCheck;

    [DataField]
    public TimeSpan DurationPerUnit = TimeSpan.FromSeconds(6);
}
