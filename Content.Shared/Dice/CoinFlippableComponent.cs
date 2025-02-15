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
    public bool CanLandOnItsSide { get; private set; } = false;

    /// <summary>
    ///     Is there an associated side sprite for it to, rarely, land on? Assume no unless otherwise set.
    /// </summary>
    [DataField]
    public bool IsWeighted { get; private set; } = false;

    /// <summary>
    ///     Percentage that it can land on its side (Only if CanLandOnItsSide is true) expressed as a float.
    ///     0.1% by default (1 in 1000 odds). It should be a rare event after all.
    /// </summary>
    [DataField]
    public float PercentageSideLand { get; private set; } = 0.1F;

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
