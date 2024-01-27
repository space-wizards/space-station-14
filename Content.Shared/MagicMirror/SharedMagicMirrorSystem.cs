using Content.Shared.DoAfter;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.MagicMirror;

[Serializable, NetSerializable]
public enum MagicMirrorUiKey
{
    Key
}

[Serializable, NetSerializable]
public enum MagicMirrorCategory
{
    Hair,
    FacialHair
}

[Serializable, NetSerializable]
public sealed class MagicMirrorSelectMessage : BoundUserInterfaceMessage
{
    public MagicMirrorSelectMessage(MagicMirrorCategory category, string marking, int slot)
    {
        Category = category;
        Marking = marking;
        Slot = slot;
    }

    public MagicMirrorCategory Category { get; }
    public string Marking { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class MagicMirrorChangeColorMessage : BoundUserInterfaceMessage
{
    public MagicMirrorChangeColorMessage(MagicMirrorCategory category, List<Color> colors, int slot)
    {
        Category = category;
        Colors = colors;
        Slot = slot;
    }

    public MagicMirrorCategory Category { get; }
    public List<Color> Colors { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class MagicMirrorRemoveSlotMessage : BoundUserInterfaceMessage
{
    public MagicMirrorRemoveSlotMessage(MagicMirrorCategory category, int slot)
    {
        Category = category;
        Slot = slot;
    }

    public MagicMirrorCategory Category { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class MagicMirrorSelectSlotMessage : BoundUserInterfaceMessage
{
    public MagicMirrorSelectSlotMessage(MagicMirrorCategory category, int slot)
    {
        Category = category;
        Slot = slot;
    }

    public MagicMirrorCategory Category { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class MagicMirrorAddSlotMessage : BoundUserInterfaceMessage
{
    public MagicMirrorAddSlotMessage(MagicMirrorCategory category)
    {
        Category = category;
    }

    public MagicMirrorCategory Category { get; }
}

[Serializable, NetSerializable]
public sealed class MagicMirrorUiData : BoundUserInterfaceMessage
{
    public MagicMirrorUiData(string species, List<Marking> hair, int hairSlotTotal, List<Marking> facialHair, int facialHairSlotTotal)
    {
        Species = species;
        Hair = hair;
        HairSlotTotal = hairSlotTotal;
        FacialHair = facialHair;
        FacialHairSlotTotal = facialHairSlotTotal;
    }

    public string Species { get; }

    public List<Marking> Hair { get; }
    public int HairSlotTotal { get; }

    public List<Marking> FacialHair { get; }
    public int FacialHairSlotTotal { get; }

}

[Serializable, NetSerializable]
public sealed partial class RemoveSlotDoAfterEvent : DoAfterEvent
{
    public MagicMirrorRemoveSlotMessage Message;

    public RemoveSlotDoAfterEvent(MagicMirrorRemoveSlotMessage message)
    {
        Message = message;
    }
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class AddSlotDoAfterEvent : DoAfterEvent
{
    public MagicMirrorAddSlotMessage Message;

    public AddSlotDoAfterEvent(MagicMirrorAddSlotMessage message)
    {
        Message = message;
    }
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class SelectDoAfterEvent : DoAfterEvent
{
    public MagicMirrorSelectMessage Message;

    public SelectDoAfterEvent(MagicMirrorSelectMessage message)
    {
        Message = message;
    }
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class ChangeColorDoAfterEvent : DoAfterEvent
{
    public MagicMirrorChangeColorMessage Message;

    public ChangeColorDoAfterEvent(MagicMirrorChangeColorMessage message)
    {
        Message = message;
    }
    public override DoAfterEvent Clone() => this;
}
