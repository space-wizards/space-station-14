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
    public readonly int Index;

    public HailerOrderMessage(int index)
    {
        Index = index;
    }
}
