using Content.Shared.Body.Organ;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part;

[Serializable, NetSerializable]
public sealed class BodyPartComponentState : ComponentState
{
    public readonly EntityUid? Body;
    public readonly BodyPartSlot? ParentSlot;
    public readonly Dictionary<string, BodyPartSlot> Children;
    public readonly Dictionary<string, OrganSlot> Organs;
    public readonly BodyPartType PartType;
    public readonly bool IsVital;
    public readonly BodyPartSymmetry Symmetry;

    public BodyPartComponentState(
        EntityUid? body,
        BodyPartSlot? parentSlot,
        Dictionary<string, BodyPartSlot> children,
        Dictionary<string, OrganSlot> organs,
        BodyPartType partType,
        bool isVital,
        BodyPartSymmetry symmetry)
    {
        ParentSlot = parentSlot;
        Children = children;
        Organs = organs;
        PartType = partType;
        IsVital = isVital;
        Symmetry = symmetry;
        Body = body;
    }
}
