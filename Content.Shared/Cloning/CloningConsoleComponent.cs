using Robust.Shared.GameStates;

namespace Content.Shared.Cloning;

/// <summary>
/// Component for cloning console UI. Stores references and state for connected cloning machines.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CloningConsoleComponent : Component
{
    /// <summary>
    /// DeviceLink source port name for medical scanner.
    /// </summary>
    public const string ScannerPort = "MedicalScannerSender";

    /// <summary>
    /// DeviceLink source port name for cloning pod.
    /// </summary>
    public const string PodPort = "CloningPodSender";

    /// <summary>
    /// EntityUid of connected genetic scanner (if any).
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? GeneticScanner = null;

    /// <summary>
    /// EntityUid of connected cloning pod (if any).
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? CloningPod = null;

    /// <summary>
    /// Maximum distance between console and one if its machines.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxDistance = 4f;

    /// <summary>
    /// Scanner in range for interaction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool GeneticScannerInRange = true;

    /// <summary>
    /// Cloning pod in range for interaction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CloningPodInRange = true;
}
