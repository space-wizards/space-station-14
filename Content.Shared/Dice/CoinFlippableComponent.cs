using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.CoinFlippable;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedCoinFlippableSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class CoinFlippableComponent : Component
{
    [DataField]
    public SoundSpecifier Sound { get; private set; } = new SoundCollectionSpecifier("Dice"); // Best SFX we got

    /// <summary>
    ///     Is there an associated side sprite for it to, rarely, land on? Assume no unless otherwise set.
    /// </summary>
    [DataField]
    public bool CanLandOnItsSide { get; private set; } = true;

    /// <summary>
    ///     Are the chances default or custom?
    ///     Default is either:
    ///         A) 50%, 50%
    ///         B) 49.5%, 49.5%, 0.1%
    ///     Depending on if CanLandOnItsSide is set or not
    /// </summary>
    [DataField]
    public bool UsesWeightedChances { get; private set; } = true;

    /// <summary>
    ///     The chance of landing on each side, expressed as a dictionary of floats.
    ///     Should add up to 100%. Will log an error otherwise.
    ///     Takes CanLandOnItsSide into consideration, ignoring the value of "side" if neccesary.
    ///
    ///     Must define in .yml:
    ///     "heads": xx.xx
    ///     "tails": xx.xx
    ///     "side": xx.xx
    /// </summary>
    [DataField]
    public Dictionary<string, float> WeightedChances { get; private set; } = new Dictionary<string, float>{};

    public Dictionary<string, float> DefaultChancesWithoutSide = new Dictionary<string, float>
    {
        { "heads", 50.0F },
        { "tails", 50.0F }
    };

    public Dictionary<string, float> DefaultChancesWithSide = new Dictionary<string, float>
    {
        { "heads", 49.95F },
        { "tails", 49.95F },
        { "side", 0.1F } // 1 in 1000 probability
    };

    /// <summary>
    ///     The currently displayed value.
    ///     0 is "Heads" (x_heads in rsi)
    ///     1 is "Tails" (x_tails in rsi)
    ///     2 is "Its Side" (x_side in rsi)
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CurrentValue { get; set; } = 1;

}
