using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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
    [DataField]
    public TimeSpan OverrideTime = TimeSpan.Zero;

    /// <summary>
    /// The ID of the itemslot that holds the law board.
    /// </summary>
    [DataField]
    public string LawBoardId = "law_board";

    /// <summary>
    /// If this tool can be used directly in the AI core to change it's laws
    /// </summary>
    [DataField]
    public bool WorksOnAiCore;

    /// <summary>
    /// If this tool can remove any corrupted laws
    /// </summary>
    [DataField]
    public bool CanChangeCorruptedLaws;
}

[Serializable, NetSerializable]
public enum LawOverriderVisuals : byte
{
    LawBoardInserted
}

[Serializable, NetSerializable]
public enum LawOverriderVisualLayers : byte
{
    Base,
    LawBoard,
    Light
}
