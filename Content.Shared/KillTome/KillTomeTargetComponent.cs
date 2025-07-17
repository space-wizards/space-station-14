using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.KillTome;

/// <summary>
/// Entity with this component is a Kill Tome target.
/// </summary>
[RegisterComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class KillTomeTargetComponent : Component
{
    [AutoNetworkedField, AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan KillTime;
}
