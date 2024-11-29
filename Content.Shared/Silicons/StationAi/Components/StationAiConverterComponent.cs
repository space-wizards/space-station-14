using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Allows for converting entities with BorgBrainComponent into AiBrain
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAiConverterComponent : Component
{
    /// <summary>
    /// The duration it takes to convert the entity to an AiBrain.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ConvertTime = 5;

    /// <summary>
    /// The text used for the popup when the used brain has no mind.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId NoMindPopup = "ai-convert-no-mind";

    /// <summary>
    /// The text used for the popup when the target entity already has an AiBrain.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId OccupiedPopup = "ai-convert-occupied";

    /// <summary>
    /// The text used for the popup when starting to convert.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId WarningPopup = "ai-convert-warning";
}
