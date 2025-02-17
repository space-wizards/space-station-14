// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Dominator;

/// <summary>
/// Enhanced version of the BatteryWeaponFireModesComponent that supports SwitchSound and UseDelay on mode switch
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DominatorSystem))]
[AutoGenerateComponentState]
public sealed partial class DominatorComponent : Component
{
    /// <summary>
    /// A list of the different firing modes the weapon can switch between
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public List<BatteryWeaponFireMode> FireModes = new();

    /// <summary>
    /// The currently selected firing mode
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CurrentFireMode;

    /// <summary>
    /// Uid of ID card that was linked to dominator using verb
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EntityUid? OwnerIdCard;

    /// <summary>
    /// Uid of idcard of last user that's hold entity
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public EntityUid? LastHoldingIdCard;

    /// <summary>
    /// Sound to play when dominator has been emagged
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");

    /// <summary>
    /// Sound thats person hears when sets owner of dominator
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier? SetOwnerSound = new SoundPathSpecifier("/Audio/_DeadSpace/Weapons/Guns/Dominator/link.ogg");

    /// <summary>
    /// Sound thats person hears when clears owner of dominator
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier? ClearOwnerSound = new SoundPathSpecifier("/Audio/_DeadSpace/Weapons/Guns/Dominator/user.ogg");

    /// <summary>
    /// Sound thats plays on low ammo
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier? LowBatterySound = new SoundPathSpecifier("/Audio/_DeadSpace/Weapons/Guns/Dominator/battery.ogg");
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BatteryWeaponFireMode
{
    /// <summary>
    /// The projectile prototype associated with this firing mode
    /// </summary>
    [DataField("proto", required: true)]
    public EntProtoId Prototype = default!;

    /// <summary>
    /// The battery cost to fire the projectile associated with this firing mode
    /// </summary>
    [DataField]
    public float FireCost = 100;

    /// <summary>
    /// Sound thats person hears when switches fire mode
    /// </summary>
    [DataField]
    public SoundSpecifier? SwitchSound;
}
