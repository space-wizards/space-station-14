using Content.Shared.Containers.ItemSlots;
using Content.Shared.Sound;

namespace Content.Shared.Icarus;

/// <summary>
/// Used for Icarus terminal activation
/// </summary>
[RegisterComponent]
public sealed class IcarusTerminalComponent : Component
{
    /// <summary>
    ///     Default fire timer value in seconds.
    /// </summary>
    [DataField("timer")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Timer = 25;

    /// <summary>
    ///     How long until the beam can arm again after fire.
    /// </summary>
    [DataField("cooldown")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Cooldown = 360;

    /// <summary>
    ///     Current status of a terminal.
    /// </summary>
    [ViewVariables]
    public IcarusTerminalStatus Status = IcarusTerminalStatus.AWAIT_DISKS;

    /// <summary>
    ///     Time until beam will be spawned in seconds.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float RemainingTime;

    /// <summary>
    ///     Time until beam cooldown will expire in seconds.
    /// </summary>
    [ViewVariables]
    public float CooldownTime;

    [DataField("alertSound")]
    public SoundSpecifier AlertSound = new SoundPathSpecifier("/Audio/Corvax/AssaultOperatives/icarus_alarm.ogg");

    [DataField("accessGrantedSound")]
    public SoundSpecifier AccessGrantedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/confirm_beep.ogg");

    [DataField("fireSound")]
    public SoundSpecifier FireSound = new SoundPathSpecifier("/Audio/Corvax/AssaultOperatives/sunbeam_fire.ogg");

    /// <summary>
    ///     Check if already notified about system authorization
    /// </summary>
    public bool AuthorizationNotified = false;

    protected override void Initialize()
    {
        base.Initialize();

        Owner.EnsureComponentWarn<ItemSlotsComponent>();
    }
}
