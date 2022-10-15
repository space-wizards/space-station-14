using Content.Shared.Body.Part;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components;

[Serializable, NetSerializable]
public sealed class BodyPartComponentState : ComponentState
{
    public readonly BodyPartSlot? ParentSlot;
    public readonly Dictionary<string, BodyPartSlot> Children;
    public readonly BodyPartType PartType;
    public readonly bool IsVital;
    public readonly BodyPartSymmetry Symmetry;
    public readonly bool Attachable;
    public readonly bool Organ;
    public readonly SoundSpecifier GibSound;

    public BodyPartComponentState(
        BodyPartSlot? parentSlot,
        Dictionary<string, BodyPartSlot> children,
        BodyPartType partType,
        bool isVital,
        BodyPartSymmetry symmetry,
        bool attachable,
        bool organ,
        SoundSpecifier gibSound)
    {
        ParentSlot = parentSlot;
        Children = children;
        PartType = partType;
        IsVital = isVital;
        Symmetry = symmetry;
        Attachable = attachable;
        Organ = organ;
        GibSound = gibSound;
    }
}