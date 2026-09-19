using Content.Shared.Botany.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Stores the active scan and update state for a plant analyzer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PlantAnalyzerSystem))]
public sealed partial class PlantAnalyzerComponent : Component
{
    /// <summary>
    /// The delay before an analyzer finishes scanning a plant.
    /// </summary>
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(0.8);

    /// <summary>
    /// Sound played when scanning finishes.
    /// </summary>
    [DataField]
    public SoundSpecifier ScanningEndSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");

    /// <summary>
    /// The plant currently being analyzed.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// The entity containing the plant data used for the current analysis.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Plant;

    /// <summary>
    /// The prototype ID of the plant used for the current analysis.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntProtoId? PlantProtoId;

    /// <summary>
    /// The user who started the current analysis.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? User;
}
