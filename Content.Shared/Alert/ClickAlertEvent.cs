using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Alert;

/// <summary>
/// A message that calls the click interaction on a alert
/// </summary>
[Serializable, NetSerializable]
public sealed class ClickAlertEvent : EntityEventArgs
{
    public readonly ProtoId<AlertPrototype> Type;

    public ClickAlertEvent(ProtoId<AlertPrototype> alertType)
    {
        Type = alertType;
    }
}

/// <summary>
/// A message that calls the right click interaction on a alert
/// </summary>
[Serializable, NetSerializable]
public sealed class RightClickAlertEvent : EntityEventArgs
{
    public readonly ProtoId<AlertPrototype> Type;

    public RightClickAlertEvent(ProtoId<AlertPrototype> alertType)
    {
        Type = alertType;
    }
}
