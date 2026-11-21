using Content.Shared.Defusable.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Defusable.Components;

/// <summary>
/// This is used for bombs that should be defused. The explosion configuration should be handled by <see cref="ExplosiveComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DefusableSystem))]
public sealed partial class DefusableComponent : Component
{
    /// <summary>
    /// The bomb will play this sound on defusal.
    /// </summary>
    [DataField]
    public SoundSpecifier DefusalSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

    /// <summary>
    /// The bomb will play this sound on bolt.
    /// </summary>
    [DataField]
    public SoundSpecifier BoltSound = new SoundPathSpecifier("/Audio/Machines/boltsdown.ogg");

    /// <summary>
    /// Is this bomb one use?
    /// </summary>
    [DataField]
    public bool Disposable = true;

    /// <summary>
    /// Is the bomb live? This is different from BombUsable because this tracks whether the bomb is ticking down or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Activated;

    /// <summary>
    /// Is the bomb actually usable? This is different from Activated because this tracks whether the bomb can even start in the first place.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Usable = true;

    /// <summary>
    /// Does the bomb show how much time remains?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool DisplayTime = true;

    /// <summary>
    /// Is this bomb supposed to be stuck to the ground?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Bolted;

    /// <summary>
    /// How much time is added when the Activate wire is pulsed?
    /// </summary>
    [DataField]
    public TimeSpan DelayTime = TimeSpan.FromSeconds(30);

    #region Wires
    // wires, this is so that they're one use
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public bool DelayWireUsed;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public bool ProceedWireCut;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public bool ProceedWireUsed;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public bool ActivatedWireUsed;

    #endregion
}
