using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.AlertLevel;

/// <summary>
/// Prototype for a single alert level a station can be set to.
/// </summary>
[Prototype]
public sealed partial class AlertLevelPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Whether this alert level is selectable from a communications console.
    /// </summary>
    [DataField]
    public bool Selectable = true;

    /// <summary>
    /// If this alert level disables user selection while it is active. Beware -
    /// setting this while something is selectable will disable selection permanently!
    /// This should only apply to entities or gamemodes that auto-select an alert level,
    /// such as a nuclear bomb being set to active.
    /// </summary>
    [DataField]
    public bool DisableSelection;

    /// <summary>
    /// The text that is announced when this alert level is selected.
    /// </summary>
    [DataField]
    public LocId? Announcement;

    /// <summary>
    /// The sound that will played in-game when this alert level is selected.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// The color that this alert level will show in-game in chat.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// The color to turn emergency lights on this station when they are active.
    /// </summary>
    [DataField]
    public Color EmergencyLightColor = Color.FromHex("#FF4020");

    /// <summary>
    /// Will this alert level force emergency lights on for the station that's active?
    /// </summary>
    [DataField]
    public bool ForceEnableEmergencyLights = false;

    /// <summary>
    /// How long it takes for the shuttle to arrive when called while this alert level is active.
    /// </summary>
    [DataField]
    public TimeSpan ShuttleTime = TimeSpan.FromMinutes(5);
}

