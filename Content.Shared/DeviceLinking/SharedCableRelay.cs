using Content.Shared.Power;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// UI key for the electro-relay configuration interface.
/// </summary>
[Serializable, NetSerializable]
public enum CableRelayUiKey : byte
{
    Key
}

/// <summary>
/// State pushed to the relay UI: whether it is powered, whether it is currently severing power,
/// and which cable types it acts on.
/// </summary>
[Serializable, NetSerializable]
public sealed class CableRelayBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool Powered;
    public bool Severed;
    public HashSet<CableType> AffectedTypes;

    public CableRelayBoundUserInterfaceState(bool powered, bool severed, HashSet<CableType> affectedTypes)
    {
        Powered = powered;
        Severed = severed;
        AffectedTypes = affectedTypes;
    }
}

/// <summary>
/// Sent when the player toggles the relay's severed state from the UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class CableRelayToggleMessage : BoundUserInterfaceMessage;

/// <summary>
/// Sent when the player enables or disables one of the cable types the relay acts on from the UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class CableRelaySetCableTypeMessage : BoundUserInterfaceMessage
{
    public CableType CableType;
    public bool Enabled;

    public CableRelaySetCableTypeMessage(CableType cableType, bool enabled)
    {
        CableType = cableType;
        Enabled = enabled;
    }
}
