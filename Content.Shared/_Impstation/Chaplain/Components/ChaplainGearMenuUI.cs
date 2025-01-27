using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Chaplain;

[Serializable, NetSerializable]
public sealed class ChaplainGearMenuBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly Dictionary<int, ChaplainGearMenuSetInfo> Sets;
    public int MaxSelectedSets;

    public ChaplainGearMenuBoundUserInterfaceState(Dictionary<int, ChaplainGearMenuSetInfo> sets, int max)
    {
        Sets = sets;
        MaxSelectedSets = max;
    }
}

[Serializable, NetSerializable]
public sealed class ChaplainGearChangeSetMessage : BoundUserInterfaceMessage
{
    public readonly int SetNumber;

    public ChaplainGearChangeSetMessage(int setNumber)
    {
        SetNumber = setNumber;
    }
}

[Serializable, NetSerializable]
public sealed class ChaplainGearMenuApproveMessage : BoundUserInterfaceMessage
{
    public ChaplainGearMenuApproveMessage() { }
}

[Serializable, NetSerializable]
public enum ChaplainGearMenuUIKey : byte
{
    Key
};

[Serializable, NetSerializable, DataDefinition]
public partial struct ChaplainGearMenuSetInfo
{
    [DataField]
    public string Name;

    [DataField]
    public string Description;

    [DataField]
    public SpriteSpecifier Sprite;

    public bool Selected;

    public ChaplainGearMenuSetInfo(string name, string desc, SpriteSpecifier sprite, bool selected)
    {
        Name = name;
        Description = desc;
        Sprite = sprite;
        Selected = selected;
    }
}
