namespace Content.Shared.Item;

/// <summary>
/// Handles whether this item is sharp when toggled on. 
/// </summary>
[RegisterComponent]
public sealed partial class ItemToggleSharpComponent : Component
{
    /// <summary>
    ///     Item can be used to butcher when activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool ActivatedSharp = true;
}



