using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.VendingMachines.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VendingMachineEjectComponent : Component
{
    /// <summary>
    /// Used by the server to determine how long the vending machine stays in the "Deny" state.
    /// Used by the client to determine how long the deny animation should be played.
    /// </summary>
    [DataField("denyDelay")]
    public TimeSpan DenyDelay = TimeSpan.FromSeconds(2.0f);

    /// <summary>
    ///    Data for understanding when the deny eject action was performed
    /// </summary>
    [DataField("denyCooldown", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan DenyCooldown = TimeSpan.Zero;

    /// <summary>
    /// Used by the server to determine how long the vending machine stays in the "Eject" state.
    /// The selected item is dispensed afer this delay.
    /// Used by the client to determine how long the deny animation should be played.
    /// </summary>
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromSeconds(1.2f);

    /// <summary>
    ///    Data for understanding when the eject action was performed
    /// </summary>
    [DataField("cooldown", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan Cooldown = TimeSpan.Zero;

    /// <summary>
    /// When true, will forcefully throw any object it dispenses
    /// </summary>
    [DataField("speedLimiter")]
    public bool CanShoot = false;

    /// <summary>
    ///     Sound that plays when ejecting an item
    /// </summary>
    [DataField("soundVend")]
    // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
    public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg")
    {
        Params = new AudioParams
        {
            Volume = -2f
        }
    };

    /// <summary>
    ///     Sound that plays when an item can't be ejected
    /// </summary>
    [DataField("soundDeny")]
    // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    public bool IsEjecting;
    public bool IsDenying;

    public string? NextItemToEject;
    public bool IsThrowNextItem = false;

    public float NonLimitedEjectForce = 7.5f;
    public float NonLimitedEjectRange = 5f;
}
