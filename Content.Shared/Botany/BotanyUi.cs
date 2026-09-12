using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany;

/// <summary>
/// Identifies the user interface opened by a plant analyzer.
/// </summary>
[Serializable, NetSerializable]
public enum PlantAnalyzerUiKey : byte
{
    Key,
}

/// <summary>
/// Identifies the user interface opened by a plant tray.
/// </summary>
[Serializable, NetSerializable]
public enum PlantTrayUiKey : byte
{
    Key,
}

/// <summary>
/// State sent to the plant analyzer user interface.
/// </summary>
[Serializable, NetSerializable]
public sealed class BotanyAnalyzerState : BoundUserInterfaceState
{
    /// <summary>
    /// The plant being analyzed.
    /// </summary>
    public NetEntity Target;

    /// <summary>
    /// The entity containing the plant data used for the analysis.
    /// </summary>
    public NetEntity? Plant;

    /// <summary>
    /// The prototype ID of the plant being analyzed.
    /// </summary>
    public EntProtoId? PlantProtoId;

    /// <summary>
    /// Localized identifiers describing persistent plant mutations.
    /// </summary>
    public List<string> Mutations = [];
}

/// <summary>
/// Completes the delayed scan performed by a plant analyzer.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class PlantAnalyzerDoAfterEvent : SimpleDoAfterEvent;
