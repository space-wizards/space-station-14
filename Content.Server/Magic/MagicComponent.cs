using System.Threading;

namespace Content.Server.Magic;

[RegisterComponent]
public sealed class MagicComponent : Component
{
    /// <summary>
    /// How long should it take for this spell to cast again.
    /// Default is 5 seconds.
    /// </summary>
    /// <returns></returns>
    [ViewVariables]
    [DataField("cooldown")]
    public float Cooldown = 5f;

    /// <summary>
    /// Does this spell need time to cast?
    /// Default is 0 seconds.
    /// </summary>
    [ViewVariables]
    [DataField("castTime")]
    public float CastTime;

    /// <summary>
    /// Can this spell be cast while moving?
    /// Default is true.
    /// </summary>
    [ViewVariables]
    [DataField("slideCast")]
    public bool SlideCast = true;

    /// <summary>
    /// How long does this spell need to be active? If applicable.
    /// Default is 0 seconds.
    /// </summary>
    [ViewVariables]
    [DataField("duration")]
    public float Duration;

    /// <summary>
    /// Cancel token for canceling certain spell DoAfters.
    /// </summary>
    public CancellationTokenSource? CancelToken;
}
