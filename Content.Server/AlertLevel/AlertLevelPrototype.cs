using System.Collections.Specialized;
using Content.Shared.Sound;
using Robust.Shared.Prototypes;

namespace Content.Server.AlertLevel;

// Alert levels.
[Prototype("alertLevel")]
public sealed class AlertLevelPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    // Dictionary of alert levels. Keyed by string - the string key is the most important
    // part here. Visualizers will use this in order to dictate what alert level to show on
    // client side sprites.
    //
    // For what it's worth, if the dictionary doesn't really change, this might just be
    // in the order that it was defined as.
    [DataField("levels")] public Dictionary<string, AlertLevelDetail> Levels = new();

    // Default level that the station is on upon initialization.
    // If this isn't in the dictionary, this will default to whatever .First() gives.
    [DataField("defaultLevel")] public string DefaultLevel { get; }= default!;
}

// Alert level detail. Does not contain an ID, that is handled by
// the Levels field in AlertLevelPrototype.
[DataDefinition]
public sealed class AlertLevelDetail
{
    // What is announced upon this alert level change. Can be a localized string.
    [DataField("announcement")] public string Announcement { get; } = default!;

    // Whether this alert level is selectable from a communications console.
    [DataField("selectable")] public bool Selectable { get; } = true;

    // If this alert level disables user selection while it is active. Beware -
    // setting this while something is selectable will disable selection permanently!
    // This should only apply to entities or gamemodes that auto-select an alert level,
    // such as a nuclear bomb being set to active.
    [DataField("disableSelection")] public bool DisableSelection { get; }

    // The sound that this alert level will play in-game once selected.
    [DataField("sound")] public SoundSpecifier? Sound { get; }

    // The color that this alert level will show in-game in chat.
    [DataField("color")] public string Color { get; } = default!;
}

