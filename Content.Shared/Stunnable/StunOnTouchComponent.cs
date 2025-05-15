using Robust.Shared.GameStates;
using Content.Shared.Stunnable;

namespace Content.Shared.Stunnable;

/// Component that allows you to add a stun on collision with anything (not just projectiles)
[RegisterComponent]
[Access(typeof(SharedStunOnTouchSystem))]
public sealed partial class StunOnTouchComponent : Component
{
    /// <summary>
    ///     How long the stun should last.
    /// </summary>
    [DataField("stunTime")]
    public float StunTime = 1f;
}
