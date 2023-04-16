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
    ///     The bomb will play this sound on bolt.
    /// </summary>
    [DataField("defusalSound")] public SoundSpecifier DefusalSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

    /// <summary>
    ///     The bomb will play this sound on bolt.
    /// </summary>
    [DataField("boltSound")] public SoundSpecifier BoltSound = new SoundPathSpecifier("/Audio/Machines/boltsdown.ogg");

    /// <summary>
    /// Is the bomb live? This is different from BombUsable because this tracks whether the bomb is ticking down or not.
    /// </summary>
    [ViewVariables, DataField("live")] public bool BombLive = false;

    /// <summary>
    /// Is the bomb actually usable? This is different from BombLive because this tracks whether the bomb can even start in the first place.
    /// </summary>
    [ViewVariables] public bool BombUsable = true;

    /// <summary>
    /// Is this bomb supposed to be stuck to the ground?
    /// </summary>
    public bool Bolted = false;

    /// <summary>
    /// How much time is added when the Activate wire is pulsed?
    /// </summary>
    [DataField("delayTime")]
    public int DelayTime = 30;

    // wires, this is so that they're one use
    public bool DelayWireCut = false;
    public bool BoltWireCut = false;
    public bool ProceedWireCut = false;
    public bool ActivatedWireCut = false;
}
