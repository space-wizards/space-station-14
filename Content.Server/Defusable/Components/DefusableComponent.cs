using Robust.Shared.Audio;

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
    [ViewVariables(VVAccess.ReadWrite), DataField("time")]
    public int StartingTime = 90;

    /// <summary>
    ///     A user can use these options to configure how long they want their bomb to be, unless null.
    /// </summary>
    [DataField("timeOptions")] public List<float>? StartingTimeOptions = null;

    /// <summary>
    ///     The bomb will play this sound while live every second, unless null.
    /// </summary>
    [DataField("beepSound")] public SoundSpecifier? BeepSound;

    /// <summary>
    ///     The bomb will play this sound on defusal, unless null.
    /// </summary>
    [DataField("defusalSound")] public SoundSpecifier? DefusalSound;

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
    [ViewVariables] public bool BombUsable = true;

    /// <summary>
    /// How much time is added when the Activate wire is pulsed?
    /// </summary>
    [ViewVariables, DataField("delayTime")]
    public int DelayTime = 30;

    // wires, this is so that they're one use
    public bool DelayWireUsed = false;
    public bool BoltWireUsed = false;
    public bool ProceedWireUsed = false;
    public bool ActivatedWireUsed = false;
}
