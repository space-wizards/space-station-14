using Robust.Shared.Serialization;

namespace Content.Shared.Disposal;

[Serializable, NetSerializable]
public enum MailingUnitUiKey : byte
{
    Key
}

/// <summary>
///     Message data sent from client to server when a disposal unit ui button is pressed.
/// </summary>
[Serializable, NetSerializable]
public sealed class TargetSelectedMessage : BoundUserInterfaceMessage
{
    public readonly string? target;

    public TargetSelectedMessage(string? target)
    {
        this.target = target;
    }
}
