using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Alert;

/// <summary>
/// A message that calls the click interaction on a alert
/// </summary>
[Serializable, NetSerializable]
public sealed class ClickAlertEvent(ProtoId<AlertPrototype> alertType) : EntityEventArgs
{
    public readonly ProtoId<AlertPrototype> Type = alertType;
}

/// <summary>
/// A message that calls the right click interaction on a alert
/// </summary>
[Serializable, NetSerializable]
public sealed class RightClickAlertEvent(ProtoId<AlertPrototype> alertType) : EntityEventArgs
{
    public readonly ProtoId<AlertPrototype> Type = alertType;
}
