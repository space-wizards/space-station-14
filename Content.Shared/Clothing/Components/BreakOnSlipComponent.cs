namespace Content.Shared.Clothing.Components;

/// <summary>
/// A component to give clothing a chance to "break" when slipping
/// </summary>
[RegisterComponent]
public sealed partial class BreakOnSlipComponent : Component
{
    /// <summary>
    /// Chance the clothing will break when slipped
    /// </summary>
    [DataField("breakChance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BreakChance = 0.01f;

    /// <summary>
    /// The slot to equip the replacement clothing to
    /// </summary>
    [DataField("slot",required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public string Slot;

    /// <summary>
    /// The clothing prototype to swap to when broken
    /// </summary>
    [DataField("replacementPrototype",required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public string ReplacementPrototype;

    /// <summary>
    /// The message to show in the popup when it breaks
    /// </summary>
    [DataField("message",required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public string Message;
}
