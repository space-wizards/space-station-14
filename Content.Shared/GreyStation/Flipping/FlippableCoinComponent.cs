using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.GreyStation.Flipping;

/// <summary>
/// A coin that can be flipped to randomly become heads or tails.
/// Has <see cref="FlippingCoinComponent"/> if it is currently being flipped.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(FlippableCoinSystem))]
[AutoGenerateComponentState]
public sealed partial class FlippableCoinComponent : Component
{
    /// <summary>
    /// Whether the coin is tails instead of heads.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Flipped;

    /// <summary>
    /// Popup to show when it's heads.
    /// </summary>
    [DataField]
    public LocId HeadsPopup = "coin-flip-heads";

    /// <summary>
    /// Popup to show when it's tails.
    /// </summary>
    [DataField]
    public LocId TailsPopup = "coin-flip-tails";

    /// <summary>
    /// Sound to play when flipping.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/GreyStation/Effects/coinflip.ogg");

    /// <summary>
    /// How long it takes to flip.
    /// </summary>
    [DataField]
    public TimeSpan FlipDelay = TimeSpan.FromSeconds(1);
}

[Serializable, NetSerializable]
public enum FlippableCoinVisuals : byte
{
    Flipped,
    Flipping
}
