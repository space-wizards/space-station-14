using Content.Shared.Arcade.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSpaceVillainArcadeSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class SpaceVillainArcadeComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultAttackSounds = "SpaceVillainAttack";

    /// <summary>
    ///
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultHealSounds = "SpaceVillainHeal";

    /// <summary>
    ///
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultChargeSounds = "SpaceVillainCharge";

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? AttackSound = new SoundCollectionSpecifier(DefaultAttackSounds);

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? HealSound = new SoundCollectionSpecifier(DefaultHealSounds);

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ChargeSound = new SoundCollectionSpecifier(DefaultChargeSounds);

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public byte PlayerHP;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public byte PlayerMaxHP = 30;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public byte PlayerMP;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public byte PlayerMaxMP = 10;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public byte VillainHP;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public byte VillainMaxHP = 45;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public byte VillainMP;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public byte VillainMaxMP = 20;
}
