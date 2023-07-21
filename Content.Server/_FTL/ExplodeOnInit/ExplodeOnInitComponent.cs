namespace Content.Server._FTL.ExplodeOnInit;

/// <summary>
/// This is used for exploding on init
/// </summary>
[RegisterComponent]
public sealed class ExplodeOnInitComponent : Component
{
    /// <summary>
    /// I know this sounds stupid. Toggles whether it should explode on init or use a timer.
    /// </summary>
    [DataField("explodeOnInit")] public bool ExplodeOnInit = true;

    [DataField("timeUntilDetonation")] public float TimeUntilDetonation = 1f;
}

