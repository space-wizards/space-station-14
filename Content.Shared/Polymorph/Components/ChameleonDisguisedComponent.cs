using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Polymorph.Components;

/// <summary>
/// Added to a player when they use a chameleon projector.
/// Handles making them invisible and revealing when damaged enough or switching hands.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChameleonDisguisedComponent : Component
{
    /// <summary>
    /// How much damage can be taken before revealing automatically.
    /// </summary>
    [DataField]
    public FixedPoint2 Integrity = FixedPoint2.Zero;

    /// <summary>
    /// The disguise entity parented to the player.
    /// </summary>
    [DataField]
    public EntityUid Disguise;

    /// <summary>
    /// For client, whether the user's sprite was previously visible or not.
    /// </summary>
    [DataField]
    public bool WasVisible;
}
