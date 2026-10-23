using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Used for gas analyzers, an item that shows players the gas contents of an atmos
/// device they use it on or of the tile they are standing on.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GasAnalyzerComponent : Component
{
    /// <summary>
    /// The target entity currently being analyzed.
    /// </summary>
    [DataField]
    public EntityUid? Target;

    /// <summary>
    /// The current user of the gas analyzer.
    /// </summary>
    [DataField]
    public EntityUid? User;

    /// <summary>
    /// Is the analyzer currently active?
    /// </summary>
    [DataField]
    public bool Enabled;
}

/// <summary>
/// Atmospheric data is gathered in the system and sent to the user.
/// </summary>
[Serializable, NetSerializable]
public sealed class GasAnalyzerUserMessage(GasMixEntry[] nodeGasMixes, string deviceName, NetEntity deviceUid, bool deviceFlipped) : BoundUserInterfaceMessage
{
    public string DeviceName = deviceName;
    public NetEntity DeviceUid = deviceUid;
    public bool DeviceFlipped = deviceFlipped;
    public GasMixEntry[] NodeGasMixes = nodeGasMixes;
}

/// <summary>
/// Contains information on a gas mix entry, turns into a tab in the UI.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct GasMixEntry(string Name, float Volume, float Pressure, float Temperature, GasEntry[]? Gases = null)
{
    /// <summary>
    /// Name of the tab in the UI.
    /// </summary>
    public readonly string Name = Name;
    /// <summary>
    /// Volume of this gas mixture.
    /// </summary>
    public readonly float Volume = Volume;
    /// <summary>
    /// Pressure of this gas mixture.
    /// </summary>
    public readonly float Pressure = Pressure;
    /// <summary>
    /// Temperature of this gas mixture.
    /// </summary>
    public readonly float Temperature = Temperature;
    /// <summary>
    /// The gases contained in this gas mixture.
    /// The gases below a certain mol threshold are not included.
    /// </summary>
    public readonly GasEntry[]? Gases = Gases;
}

/// <summary>
/// Individual gas entry data for populating the UI.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct GasEntry(Gas Gas, float Amount)
{
    /// <summary>
    /// The gas this entry represents.
    /// </summary>
    public readonly Gas Gas = Gas;
    /// <summary>
    /// The gas amount in mol.
    /// </summary>
    public readonly float Amount = Amount;
}

/// <summary>
/// Key for the GasAnalyzerBoundUserInterface.
/// </summary>
[Serializable, NetSerializable]
public enum GasAnalyzerUiKey
{
    Key,
}

/// <summary>
/// Individual gas entry data for populating the UI
/// </summary>
[Serializable, NetSerializable]
public enum GasAnalyzerVisuals : byte
{
    Enabled,
}

