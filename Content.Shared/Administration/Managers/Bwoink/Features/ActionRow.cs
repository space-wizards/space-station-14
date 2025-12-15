namespace Content.Shared.Administration.Managers.Bwoink.Features;

/// <summary>
/// Adds a row of buttons to the bwoink UI.
/// </summary>
/// <remarks>
/// Can have multiple rows, might look weird though.
/// </remarks>
public sealed partial class ActionRow : BwoinkChannelFeature
{
    /// <summary>
    /// The buttons contained in this row.
    /// </summary>
    [DataField(required: true)]
    public List<ActionButton> Buttons { get; set; } = new List<ActionButton>();
}

/// <summary>
/// Allows clients to load custom action rows via YML.
/// </summary>
public sealed partial class AllowClientActionRow : BwoinkChannelFeature;

/// <summary>
/// A button in an <see cref="ActionRow"/>
/// </summary>
[DataDefinition]
public sealed partial class ActionButton
{
    /// <summary>
    /// The button label.
    /// </summary>
    [DataField(required: true)]
    public LocId Label { get; set; }

    /// <summary>
    /// The command to execute on press.
    /// </summary>
    /// <remarks>
    /// Supports the following replacements:
    /// $ID, selected player ID.
    /// $Name, selected player name.
    /// $SelfID, the id of you
    /// $SelfName, the name of you.
    /// $NetEnt, selected network entity
    /// </remarks>
    [DataField(required: true)]
    public string Command { get; set; }

    /// <summary>
    /// If you should need to click to confirm.
    /// </summary>
    [DataField]
    public bool Confirm { get; set; } = false;

    /// <summary>
    /// Flags required to execute the underlying command.
    /// </summary>
    [DataField(required: true)]
    public AdminFlags Flags { get; set; }

    /// <summary>
    /// If this button should always be available. Usually the buttons only work when a player is selected.
    /// If you set this to true you are making the contract of "my command will NOT use the selected player info."
    /// break that contract and i will murder you.
    /// </summary>
    [DataField]
    public bool AlwaysAvailable { get; set; } = false;
}
