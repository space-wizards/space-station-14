namespace Content.Server.Defusable.Components;

/// <summary>
/// This is used for bombs that should be defused.
/// </summary>
[RegisterComponent]
public sealed class DefusableComponent : Component
{
    // most of the actual explosion stuff is handled by ExplosiveComponent

    /// <summary>
    /// The time set by the player.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public int StartingTime = 90;

    /// <summary>
    /// The actual time until the bomb explodes.
    /// </summary>
    [ViewVariables] public int TimeUntilExplosion = 90;

    /// <summary>
    /// Is the bomb live? This is different from BombUsable because this tracks whether the bomb is ticking down or not.
    /// </summary>
    [ViewVariables, DataField("live")] public bool BombLive = false;

    /// <summary>
    /// Is the bomb actually usable? This is different from BombLive because this tracks whether the bomb can even start in the first place.
    /// </summary>
    [ViewVariables] public bool BombUsable = false;

    /// <summary>
    /// How much time is added when the Activate wire is pulsed?
    /// </summary>
    [ViewVariables, DataField("delayTime")]
    public int DelayTime = 30;

    // wires, this is so that they're one use
    public bool DelayWireUsed = false;
    public bool BoltWireUsed = false;
    public bool ActivateWireUsed = false;
    public bool ProceedWireUsed = false;
}
