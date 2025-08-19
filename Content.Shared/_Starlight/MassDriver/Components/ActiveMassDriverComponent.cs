using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Starlight.MassDriver.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class ActiveMassDriverComponent : Component
{
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;
}