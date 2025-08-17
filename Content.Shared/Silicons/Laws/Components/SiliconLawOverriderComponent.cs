namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This changes the laws of a entity with law provider using a law board as base
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SiliconLawOverriderComponent : Component
{
    /// <summary>
    /// Duration of the doafter after using this tool
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan OverrideTime = TimeSpan.Zero;

    /// <summary>
    /// The ID of the itemslot that holds the law board.
    /// </summary>
    [DataField("LawBoardId")]
    public string LawBoardId = "law_board";
}
