using Content.Server.Defusable.Systems;
using Content.Server.Explosion.Components;
using Robust.Shared.Audio;

namespace Content.Server.Defusable.Components;

/// <summary>
/// This is used for bombs that should be defused. The explosion configuration should be handled by <see cref="ExplosiveComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(DefusableSystem))]
public sealed partial class DefusableComponent : Component
{
    /// <summary>
    ///     The bomb will play this sound on defusal.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("defusalSound")]
    public SoundSpecifier DefusalSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

    /// <summary>
    ///     The bomb will play this sound on bolt.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("boltSound")]
    public SoundSpecifier BoltSound = new SoundPathSpecifier("/Audio/Machines/boltsdown.ogg");

    /// <summary>
    ///     Is this bomb one use?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("disposable")]
    public bool Disposable = true;

    /// <summary>
    /// Is the bomb live? This is different from BombUsable because this tracks whether the bomb is ticking down or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("activated")]
    public bool Activated;

    /// <summary>
    /// Is the bomb actually usable? This is different from Activated because this tracks whether the bomb can even start in the first place.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Usable = true;

    /// <summary>
    /// Does the bomb show how much time remains?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool DisplayTime = true;

    /// <summary>
    /// Is this bomb supposed to be stuck to the ground?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Bolted;

    /// <summary>
    /// How much time is added when the Activate wire is pulsed?
    /// </summary>
    [DataField("delayTime")]
    public int DelayTime = 30;

    #region Wires
    // wires, this is so that they're one use
    [ViewVariables(VVAccess.ReadWrite), Access(Other=AccessPermissions.ReadWrite)]
    public bool DelayWireUsed;

    [ViewVariables(VVAccess.ReadWrite), Access(Other=AccessPermissions.ReadWrite)]
    public bool ProceedWireCut;

    [ViewVariables(VVAccess.ReadWrite), Access(Other=AccessPermissions.ReadWrite)]
    public bool ProceedWireUsed;

    [ViewVariables(VVAccess.ReadWrite), Access(Other=AccessPermissions.ReadWrite)]
    public bool ActivatedWireUsed;

    #endregion
}
