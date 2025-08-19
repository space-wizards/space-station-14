using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Starlight.MassDriver.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class ActiveMassDriverComponent : Component
{
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);

    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    [DataField("nextThrow", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextThrowTime;
}