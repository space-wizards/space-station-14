namespace Content.Server.Item;

/// <summary>
/// Handles whether this item is sharp when toggled on. 
/// </summary>
[RegisterComponent]
public sealed partial class ItemToggleSharpComponent : Component
{
    /// <summary>
    ///     Item can be used to butcher when activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public bool ActivatedSharp = true;
}
