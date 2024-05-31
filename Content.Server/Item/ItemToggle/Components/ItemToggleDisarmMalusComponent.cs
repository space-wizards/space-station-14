namespace Content.Server.Item;

/// <summary>
/// Handles whether this item applies a disarm malus when active.
/// </summary>
[RegisterComponent]
public sealed partial class ItemToggleDisarmMalusComponent : Component
{
    /// <summary>
    ///     Item has this modifier to the chance to disarm when activated.
    ///     If null, the value will be inferred from the current malus just before the malus is first deactivated.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public float? ActivatedDisarmMalus = null;

    /// <summary>
    ///     Item has this modifier to the chance to disarm when deactivated. If none is mentioned, it uses the item's default disarm modifier.
    ///     If null, the value will be inferred from the current malus just before the malus is first activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public float? DeactivatedDisarmMalus = null;
}
