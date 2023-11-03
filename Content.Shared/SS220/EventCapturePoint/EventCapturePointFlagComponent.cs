// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.EventCapturePoint;

/// <summary>
/// Component that ensures that an entity can be used for capturing an event point.
/// </summary>
[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class EventCapturePointFlagComponent : Component
{
    [ViewVariables, DataField, AutoNetworkedField]
    public bool Planted;
}

[Serializable, NetSerializable]
public enum CaptureFlagVisuals
{
    Visuals,
}
