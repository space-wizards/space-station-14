using Content.Shared.Dataset;
using Content.Server.Arcade.EntitySystems.SpaceVillain;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server.Arcade.Components.SpaceVillain;

/// <summary>
///
/// </summary>
[RegisterComponent, Access(typeof(SpaceVillainArcadeSystem))]
public sealed partial class SpaceVillainArcadeComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField]
    public bool PlayerInvincible;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public int PlayerMaxHP = 30;

    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int PlayerHP = 0;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public int PlayerMaxMP = 10;

    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int PlayerMP = 0;

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype>? VillainFirstNames;

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype>? VillainLastNames;

    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string VillainName = "Villain";

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public bool VillainInvincible;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public int VillainMaxHP = 45;

    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int VillainHP = 0;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public int VillainMaxMP = 20;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public int VillainMP = 0;

    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int HealTracker = 0;

    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string PlayerStatus = string.Empty;

    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string VillainStatus = string.Empty;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public SoundSpecifier AttackSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_attack.ogg");

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public SoundSpecifier HealSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_heal.ogg");

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public SoundSpecifier RechargeSound = new SoundPathSpecifier("/Audio/Effects/Arcade/player_charge.ogg");
}
