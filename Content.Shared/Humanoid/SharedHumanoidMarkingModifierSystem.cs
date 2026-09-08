using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[Serializable, NetSerializable]
public enum HumanoidMarkingModifierKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class HumanoidMarkingModifierMarkingSetMessage : BoundUserInterfaceMessage
{
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings { get; }

    public HumanoidMarkingModifierMarkingSetMessage(Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings)
    {
        Markings = markings;
    }
}

[Serializable, NetSerializable]
public sealed class HumanoidMarkingModifierState : BoundUserInterfaceState
{
    public HumanoidMarkingModifierState(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings,
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> organData,
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> organProfileData
    )
    {
        Markings = markings;
        OrganData = organData;
        OrganProfileData = organProfileData;
    }

    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings { get; }
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> OrganData { get; }
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> OrganProfileData { get; }
}
