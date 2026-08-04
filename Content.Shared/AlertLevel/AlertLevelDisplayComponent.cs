using Robust.Shared.Prototypes;

namespace Content.Shared.AlertLevel;

/// <summary>
/// Allows an entity to change visuals depending on the station's current alertlevel.
/// </summary>
[RegisterComponent]
public sealed partial class AlertLevelDisplayComponent : Component
{
    /// <summary>
    /// The RSI state to use for each alert level.
    /// Changes the <see cref="AlertLevelDisplay.Layer"/> layer accordingly.
    /// If the device is unpowered that layer will be hidden.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AlertLevelPrototype>, string> AlertVisuals = new();
}
