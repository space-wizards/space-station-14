using Content.Shared.CosmicCult.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Added to entities to tag that they are a cosmic cultist. Holds nearly all cultist-relevant data!
/// Yeah, that means it's kinda a god component. 162 lines! Look at it go!
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CosmicCultistComponent : Component
{
    /// <summary>
    /// The status icon prototype displayed for cosmic cultists.
    /// </summary>
    [DataField] public ProtoId<FactionIconPrototype> StatusIcon = "CosmicCultFaction";

    public int ProgressGoal = 10;

    /// <summary>
    /// How much progress this cultist personally has towards gaining their next Influence.
    /// </summary>
    [DataField, AutoNetworkedField] public int PersonalProgress;

    /// <summary>
    /// How many times this cultist is allowed to visit The Monument to gain a new Influence.
    /// </summary>
    [DataField, AutoNetworkedField] public int MonumentVisits;

    /// <summary>
    /// Owned and unlocked influences.
    /// </summary>
    [DataField, AutoNetworkedField] public List<ProtoId<InfluencePrototype>> OwnedInfluences = [];

    [DataField, AutoNetworkedField] public Dictionary<ProtoId<InfluencePrototype>, float> UnlockedInfluences = [];

    #region Ability Data
    [DataField] public EntProtoId CosmicFragmentationAction = "ActionCosmicFragmentation";

    /// <summary>
    /// The duration of the doAfters and time away for Astral Shift.
    /// </summary>
    [DataField] public TimeSpan CosmicShiftInOut = TimeSpan.FromSeconds(2);
    [DataField] public TimeSpan CosmicShiftWindup = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan DefaultCosmicShiftWindup = TimeSpan.FromSeconds(3);

    #endregion

    #region Misc Data
    /// <summary>
    /// How many stacks of Astral Aegis this cultist has.
    /// </summary>
    [DataField, AutoNetworkedField] public int AstralAegisStacks;

    /// <summary>
    /// Wether or not this cultist has been empowered by a Malign Rift.
    /// </summary>
    [DataField, AutoNetworkedField] public bool CosmicEmpowered;
    /// <summary>
    /// Wether or not this cultist was previously empowered by a Malign Rift.
    /// </summary>
    [DataField, AutoNetworkedField] public bool WasEmpowered;

    /// <summary>
    /// Wether or not this cultist needs to respirate.
    /// </summary>
    [DataField, AutoNetworkedField] public bool Respiration = true;
    #endregion

    /// <summary>
    ///     The gamerule that this cultist is associated with
    /// </summary>
    [DataField(serverOnly: true)]
    public EntityUid CultGamerule;

    #region VFX & SFX
    [DataField] public EntProtoId ShuntVfx = "CosmicShuntAbilityVfx";

    [DataField] public SoundSpecifier MonumentGachaSfx = new SoundPathSpecifier("/Audio/Cosmic/monument-gacha.ogg");
    [DataField] public SoundSpecifier AegisDeflectSfx = new SoundPathSpecifier("/Audio/Cosmic/cosmicsword-glance.ogg");
    [DataField] public SoundSpecifier AbilityGainSfx = new SoundPathSpecifier("/Audio/Cosmic/Abilities/ability-gained.ogg");
    [DataField] public SoundSpecifier ShuntSfx = new SoundPathSpecifier("/Audio/Cosmic/Abilities/ability-shunt.ogg");
    #endregion
}
