using Content.Shared.DoAfter;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.MagicMirror;

[Serializable, NetSerializable]
public enum MagicMirrorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum MagicMirrorCategory : byte
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
public sealed class MagicMirrorUiState : BoundUserInterfaceState
{
    public MagicMirrorUiState(string species, List<Marking> hair, int hairSlotTotal, List<Marking> facialHair, int facialHairSlotTotal)
    {
        Species = species;
        Hair = hair;
        HairSlotTotal = hairSlotTotal;
        FacialHair = facialHair;
        FacialHairSlotTotal = facialHairSlotTotal;
    }

    public NetEntity Target;

    public string Species;

    public List<Marking> Hair;
    public int HairSlotTotal;

    public List<Marking> FacialHair;
    public int FacialHairSlotTotal;
}

[Serializable, NetSerializable]
public sealed partial class MagicMirrorRemoveSlotDoAfterEvent : DoAfterEvent
{
    public MagicMirrorRemoveSlotMessage Message;

    public MagicMirrorRemoveSlotDoAfterEvent(MagicMirrorRemoveSlotMessage message)
    {
        Message = message;
    }
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class MagicMirrorAddSlotDoAfterEvent : DoAfterEvent
{
    public MagicMirrorAddSlotMessage Message;

    public MagicMirrorAddSlotDoAfterEvent(MagicMirrorAddSlotMessage message)
    {
        Message = message;
    }
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class MagicMirrorSelectDoAfterEvent : DoAfterEvent
{
    public MagicMirrorSelectMessage Message;

    public MagicMirrorSelectDoAfterEvent(MagicMirrorSelectMessage message)
    {
        Message = message;
    }
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class MagicMirrorChangeColorDoAfterEvent : DoAfterEvent
{
    public MagicMirrorChangeColorMessage Message;

    public MagicMirrorChangeColorDoAfterEvent(MagicMirrorChangeColorMessage message)
    {
        Message = message;
    }
    public override DoAfterEvent Clone() => this;
}
