using Content.Shared.Markings;
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
    public MagicMirrorSelectMessage(MagicMirrorCategory category, string marking, uint slot)
    {
        Category = category;
        Marking = marking;
        Slot = slot;
    }

    public MagicMirrorCategory Category { get; }
    public string Marking { get; }
    public uint Slot { get; }
}

[Serializable, NetSerializable]
public sealed class MagicMirrorChangeColorMessage : BoundUserInterfaceMessage
{
    public MagicMirrorChangeColorMessage(MagicMirrorCategory category, List<Color> colors, uint slot)
    {
        Category = category;
        Colors = colors;
        Slot = slot;
    }

    public MagicMirrorCategory Category { get; }
    public List<Color> Colors { get; }
    public uint Slot { get; }
}

[Serializable, NetSerializable]
public sealed class MagicMirrorRemoveSlotMessage : BoundUserInterfaceMessage
{
    public MagicMirrorRemoveSlotMessage(MagicMirrorCategory category, uint slot)
    {
        Category = category;
        Slot = slot;
    }

    public MagicMirrorCategory Category { get; }
    public uint Slot { get; }
}

[Serializable, NetSerializable]
public sealed class MagicMirrorSelectSlotMessage : BoundUserInterfaceMessage
{
    public MagicMirrorSelectSlotMessage(MagicMirrorCategory category, uint slot)
    {
        Category = category;
        Slot = slot;
    }

    public MagicMirrorCategory Category { get; }
    public uint Slot { get; }
}

[Serializable, NetSerializable]
public sealed class MagicMirrorAddSlotMessage : BoundUserInterfaceMessage
{
    public MagicMirrorAddSlotMessage(MagicMirrorAddSlotMessage category)
    {
        Category = category;
    }

    public MagicMirrorAddSlotMessage Category { get; }
}

[Serializable, NetSerializable]
public sealed class MagicMirrorUiState : BoundUserInterfaceState
{
    public MagicMirrorUiState(string species, Marking hair, uint hairSlot, uint hairSlotsUsed, uint hairSlotTotal, Marking facialHair, uint facialHairSlot, uint facialHairSlotsUsed, uint facialHairSlotTotal)
    {
        Species = species;
        Hair = hair;
        HairSlot = hairSlot;
        HairSlotsUsed = hairSlotsUsed;
        HairSlotTotal = hairSlotTotal;
        FacialHair = facialHair;
        FacialHairSlot = facialHairSlot;
        FacialHairSlotsUsed = facialHairSlotsUsed;
        FacialHairSlotTotal = facialHairSlotTotal;
    }

    public string Species { get; }

    public Marking Hair { get; }
    public uint HairSlot { get; }
    public uint HairSlotsUsed { get; }
    public uint HairSlotTotal { get; }

    public Marking FacialHair { get; }
    public uint FacialHairSlot { get; }
    public uint FacialHairSlotsUsed { get; }
    public uint FacialHairSlotTotal { get; }

}
