using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Materials;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedMaterialReclaimerSystem))]
public sealed class ActiveMaterialReclaimerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public Container ReclaimingContainer = default!;

    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime;

    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration;
}
