namespace Content.Server.Item;

/// <summary>
/// Handles whether this item applies a disarm malus when active. 
/// </summary>
[RegisterComponent]
public sealed partial class ItemToggleDisarmMalusComponent : Component
{
    /// <summary>
    ///     Item has this modifier to the chance to disarm when activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public float? ActivatedDisarmMalus = null;

    /// <summary>
    ///     Item has this modifier to the chance to disarm when deactivated. If none is mentioned, it uses the item's default disarm modifier.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public float? DeactivatedDisarmMalus = null;
}
