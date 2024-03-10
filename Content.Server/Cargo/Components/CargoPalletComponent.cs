namespace Content.Server.Cargo.Components;

/// <summary>
/// Any entities intersecting when a shuttle is recalled will be sold.
/// </summary>
[RegisterComponent]
public sealed partial class CargoPalletComponent : Component
{
    /// <summary>
    /// Whether the pad is a buy pad, a sell pad, or both.
    /// </summary>
    [DataField]
    public string PalletType;
}
