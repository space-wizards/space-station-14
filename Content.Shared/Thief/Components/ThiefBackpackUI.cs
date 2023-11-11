using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Thief;

[Serializable, NetSerializable]
public sealed class ThiefBackpackBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<ThiefBackpackSetInfo> Sets;

    public ThiefBackpackBoundUserInterfaceState(List<ThiefBackpackSetInfo> sets)
    {
        Sets = sets;
    }
}

[Serializable, NetSerializable]
public sealed class ThiefBackpackChangeSetMessage : BoundUserInterfaceMessage
{
    public readonly int SetNumber;

    public ThiefBackpackChangeSetMessage(int setNumber)
    {
        SetNumber = setNumber;
    }
}

[Serializable, NetSerializable]
public sealed class ThiefBackpackApproveMessage : BoundUserInterfaceMessage
{
    public ThiefBackpackApproveMessage() { }
}

[Serializable, NetSerializable]
public enum ThiefBackpackUIKey
{
    Key
};

[Serializable, NetSerializable, DataDefinition]
public partial struct ThiefBackpackSetInfo
{
    //[DataField]
    //public ProtoId<ThiefBackpackSetPrototype> proto = default!;

    [DataField]
    public string Name;

    [DataField]
    public string Description;

    [DataField]
    public SpriteSpecifier Sprite;

    public bool Selected;

    public ThiefBackpackSetInfo(string name, string desc, SpriteSpecifier sprite, bool selected)
    {
        Name = name;
        Description = desc;
        Sprite = sprite;
        Selected = selected;
    }
}
