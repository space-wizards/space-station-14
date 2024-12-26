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
