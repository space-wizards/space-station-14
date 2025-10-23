using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

[Serializable, NetSerializable]
public enum HailerUiKey : byte
{
    Key
}

/// <summary>
/// Message to try play a hailer line.
/// </summary>
[Serializable, NetSerializable]
public sealed class HailerOrderMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// Index for the hailer order choosen by the user
    /// </summary>
    public readonly int OrderIndex;

    public HailerOrderMessage(int index)
    {
        OrderIndex = index;
    }
}
