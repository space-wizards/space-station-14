using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Surgery;

[RegisterComponent]
public sealed partial class SurgeryGuideTargetComponent : Component
{
    [DataField]
    public string Category = "Surgery";
}

[Serializable, NetSerializable]
public enum SurgeryGuideUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SurgeryGuideStartSurgeryMessage(ProtoId<ConstructionPrototype> prototype) : BoundUserInterfaceMessage
{
    public ProtoId<ConstructionPrototype> Prototype = prototype;
}

[Serializable, NetSerializable]
public sealed class SurgeryGuideStartCleanupMessage() : BoundUserInterfaceMessage;
