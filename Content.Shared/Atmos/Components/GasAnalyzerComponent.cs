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
    public EntityUid User;

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
public struct GasMixEntry(string name, float volume, float pressure, float temperature, GasEntry[]? gases = null)
{
    /// <summary>
    /// Name of the tab in the UI.
    /// </summary>
    public readonly string Name = name;
    /// <summary>
    /// Volume of this gas mixture.
    /// </summary>
    public readonly float Volume = volume;
    /// <summary>
    /// Pressure of this gas mixture.
    /// </summary>
    public readonly float Pressure = pressure;
    /// <summary>
    /// Temperature of this gas mixture.
    /// </summary>
    public readonly float Temperature = temperature;
    /// <summary>
    /// The gases contained in this gas mixture.
    /// The gases below a certain mol threshold are not included.
    /// </summary>
    public readonly GasEntry[]? Gases = gases;
}

/// <summary>
/// Individual gas entry data for populating the UI.
/// </summary>
[Serializable, NetSerializable]
public readonly struct GasEntry(Gas gas, float amount)
{
    /// <summary>
    /// The gas this entry represents.
    /// </summary>
    public readonly Gas Gas = gas;
    /// <summary>
    /// The gas amount in mol.
    /// </summary>
    public readonly float Amount = amount;
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

