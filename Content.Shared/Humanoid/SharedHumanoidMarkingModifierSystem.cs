using Content.Shared.Humanoid.Markings;
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
    public MarkingSet MarkingSet { get; }
    public bool ResendState { get; }

    public HumanoidMarkingModifierMarkingSetMessage(MarkingSet set, bool resendState)
    {
        MarkingSet = set;
        ResendState = resendState;
    }
}

[Serializable, NetSerializable]
public sealed class HumanoidMarkingModifierState : BoundUserInterfaceState
{
    public HumanoidMarkingModifierState(MarkingSet markingSet, string species, Color skinColor)
    {
        MarkingSet = markingSet;
        Species = species;
        SkinColor = skinColor;
    }

    public MarkingSet MarkingSet { get; }
    public string Species { get; }
    public Color SkinColor { get; }
}
