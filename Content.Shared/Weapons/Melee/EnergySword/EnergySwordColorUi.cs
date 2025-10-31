using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.EnergySword;

[Serializable, NetSerializable]
public enum EnergySwordColorUiKey : byte
{
    Key
}


[Serializable, NetSerializable]
public sealed class EnergySwordColorMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// Color of the blade
    /// </summary>
    public readonly Color ChoosenColor;

    /// <summary>
    /// Should the blade be RGB cycling
    /// </summary>
    public readonly bool RGB;

    public EnergySwordColorMessage(Color color, bool rgb = false)
    {
        ChoosenColor = color;
        RGB = rgb;
    }
}
