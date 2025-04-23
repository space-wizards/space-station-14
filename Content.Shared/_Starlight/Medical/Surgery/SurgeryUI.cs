using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
[Serializable, NetSerializable]
public enum SurgeryUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SurgeryBuiState : BoundUserInterfaceState
{
    public required Dictionary<NetEntity, List<(EntProtoId, string, bool)>> Choices { get; init; }
}

[Serializable, NetSerializable]
public sealed class SurgeryStepChosenBuiMsg : BoundUserInterfaceMessage
{
    public required NetEntity Part { get; init; }
    public required EntProtoId Surgery { get; init; }
    public required EntProtoId Step { get; init; }
}
