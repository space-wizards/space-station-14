namespace Content.Server.Cargo.Components;
using Content.Shared.Actions;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

/// <summary>
/// Any entities intersecting when a shuttle is recalled will be sold.
/// </summary>
[RegisterComponent]
public sealed partial class CargoPalletComponent : Component
{
    /// <summary>
    /// Whether the pad is a buy pad, a sell pad, or both.
    /// </summary>

    public enum BuySellType
    {
        buy,
        sell,
        both
    }

    [DataField]
    public BuySellType PalletType;
}
