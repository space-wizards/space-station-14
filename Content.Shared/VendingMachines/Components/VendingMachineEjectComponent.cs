using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.VendingMachines.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true), AutoGenerateComponentPause]
public sealed partial class VendingMachineEjectComponent : Component
{
    /// <summary>
    /// Used by the server to determine how long the vending machine stays in the "Deny" state.
    /// Used by the client to determine how long the deny animation should be played.
    /// </summary>
    [DataField]
    public TimeSpan DenyDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Used by the server to determine how long the vending machine stays in the "Eject" state.
    /// The selected item is dispensed after this delay.
    /// Used by the client to determine how long the eject animation should be played.
    /// </summary>
    [DataField]
    public TimeSpan EjectDelay = TimeSpan.FromSeconds(1.2);

    [ViewVariables]
    public bool Ejecting => EjectEnd != null;

    [ViewVariables]
    public bool Denying => DenyEnd != null;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? EjectEnd;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? DenyEnd;

    public string? NextItemToEject;

    /// <summary>
    /// When true, will forcefully throw any object it dispenses.
    /// </summary>
    [DataField]
    public bool CanShoot;

    public bool ThrowNextItem;

    /// <summary>
    /// Sound that plays when ejecting an item.
    /// </summary>
    [DataField]
    // Grabbed from: https://github.com/tgstation/tgstation/blob/d34047a5ae911735e35cd44a210953c9563caa22/sound/machines/machine_vend.ogg
    public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg")
    {
        Params = new AudioParams
        {
            Volume = -4f,
            Variation = 0.15f
        }
    };

    /// <summary>
    /// Sound that plays when an item can't be ejected.
    /// </summary>
    [DataField]
    // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    public float NonLimitedEjectForce = 7.5f;

    public float NonLimitedEjectRange = 5f;
}
