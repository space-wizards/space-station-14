namespace Content.Server.Cargo.Components;

/// <summary>
/// Any entities intersecting when a shuttle is recalled will be sold.
/// </summary>

[Flags]
public enum BuySellType : byte
{
    Buy = 1 << 0,
    Sell = 1 << 1,
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

    /// <summary>
    /// How many seconds does a hacking beacon need to be planted to this to successfully hijack the ATS?
    /// </summary>
    [DataField]
    public TimeSpan HackCompletionTime = TimeSpan.FromSeconds(200);

    /// <summary>
    /// How much cash should be withdrawn from each department account upon a hijacking?
    /// </summary>
    [DataField]
    public int Fine = 5000;
}
