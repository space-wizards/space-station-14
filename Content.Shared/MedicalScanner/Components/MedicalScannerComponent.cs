using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner.Components;

/// <summary>
/// Component for the medical scanner machine. Tracks scanned body state and linkage.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MedicalScannerComponent : Component
{
    /// <summary>
    /// DeviceLink sink port identifier for this scanner.
    /// </summary>
    public const string ScannerPort = "MedicalScannerReceiver";

    /// <summary>
    /// Slot containing the body being scanned.
    /// </summary>
    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// Reference to the connected console, if any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ConnectedConsole;

    /// <summary>
    /// Multiplier for the chance to fail cloning from this scanner.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CloningFailChanceMultiplier = 1f;
}

/// <summary>
/// Visual state values for medical scanner UI.
/// </summary>
[Serializable, NetSerializable]
public enum MedicalScannerVisuals : byte
{
    Status
}

/// <summary>
/// Possible state/status codes for the medical scanner.
/// </summary>
[Serializable, NetSerializable]
public enum MedicalScannerStatus : byte
{
    Off,
    Open,
    Red,
    Death,
    Green,
    Yellow,
}
