namespace Content.Server.Cargo.Components;
using Content.Shared.Actions;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

/// <summary>
/// Any entities intersecting when a shuttle is recalled will be sold.
/// </summary>

public enum BuySellType
{
    Buy = 1,
    Sell = 2,
    All = Buy | Sell
}


[RegisterComponent]
public sealed partial class CargoPalletComponent : Component
{
    /// <summary>
    /// Whether the pad is a buy pad, a sell pad, or all.
    /// </summary>
    [DataField]
    public BuySellType PalletType;
}
