// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkReaper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDarkReaperSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class DarkReaperComponent : Component
{
    public const string ConsumedContainerId = "consumed";

    [ViewVariables, DataField]
    public EntProtoId PortalEffectPrototype = "DarkReaperPortalEffect";

    /// <summary>
    /// Wheter the Dark Reaper is currently in physical form or not.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool PhysicalForm = false;

    /// <summary>
    /// Max progression stage
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public int MaxStage = 3;

    /// <summary>
    /// Progression stage
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int CurrentStage = 1;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int Consumed = 0;

    /// DEATH ///

    /// <summary>
    /// Sound played when reaper dies
    /// </summary>
    [ViewVariables, DataField]
    public SoundSpecifier SoundDeath = new SoundPathSpecifier("/Audio/SS220/DarkReaper/jnec_dead.ogg", new()
    {
        MaxDistance = 9
    });

    [ViewVariables, DataField]
    public SoundSpecifier HitSound = new SoundCollectionSpecifier("DarkReaperHit");

    [ViewVariables, DataField]
    public SoundSpecifier SwingSound = new SoundCollectionSpecifier("DarkReaperSwing");

    [ViewVariables, DataField(serverOnly: true)]
    public HashSet<EntProtoId> SpawnOnDeathPool = new()
    {
        "LeftLegHuman",
        "RightLegHuman",
        "LeftFootHuman",
        "RightFootHuman",
        "LeftArmHuman",
        "RightArmHuman",
        "LeftHandHuman",
        "RightHandHuman",
        "TorsoSkeleton",
        "NormalHeadSkeleton"
    };

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int SpawnOnDeathAmount = 8;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int SpawnOnDeathAdditionalPerStage = 4;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float SpawnOnDeathImpulseStrength = 30;


    /// ABILITY STATS ///

    /// STUN

    /// <summary>
    /// For how long reaper emites a bright red glow
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public TimeSpan StunGlareLength = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Stun ability radius, in tiles
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public float StunAbilityRadius = 3;

    /// <summary>
    /// Radius in which stun ability breaks lights
    /// </summary>
    [ViewVariables, DataField]
    public float StunAbilityLightBreakRadius = 4.5f;

    /// <summary>
    /// Duration of the stun that is applied by the ability
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(4);

    [ViewVariables, DataField, AutoNetworkedField]
    public string LightBehaviorFlicker = "flicker";

    /// <summary>
    /// Stun ability sound
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public SoundSpecifier StunAbilitySound = new SoundPathSpecifier("/Audio/SS220/DarkReaper/jnec_scrm.ogg");

    /// ROFL

    /// <summary>
    /// Rofl sound
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public SoundSpecifier RolfAbilitySound = new SoundPathSpecifier("/Audio/SS220/DarkReaper/jnec_rolf.ogg", new()
    {
        MaxDistance = 7
    });

    /// MATERIALIZE

    /// <summary>
    /// How long reaper can stay materialized, depending on stage
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public List<TimeSpan> MaterializeDurations = new()
    {
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(40)
    };

    public TimeSpan CooldownAfterMaterialize = TimeSpan.FromSeconds(3);

    [ViewVariables, DataField, AutoNetworkedField]
    public float MaterialMovementSpeed = 4f;

    [ViewVariables, DataField, AutoNetworkedField]
    public float UnMaterialMovementSpeed = 7f;

    /// <summary>
    /// Sound played before portal opens
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public SoundSpecifier PortalOpenSound = new SoundPathSpecifier("/Audio/SS220/DarkReaper/jnec_gate_open.ogg", new()
    {
        MaxDistance = 8
    });

    /// <summary>
    /// Sound played before portal closes
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public SoundSpecifier PortalCloseSound = new SoundPathSpecifier("/Audio/SS220/DarkReaper/jnec_gate_close.ogg", new()
    {
        MaxDistance = 7
    });

    /// CONSOOM

    [ViewVariables, DataField, AutoNetworkedField]
    public SoundSpecifier ConsumeAbilitySound = new SoundPathSpecifier("/Audio/SS220/DarkReaper/jnec_eat.ogg", new()
    {
        MaxDistance = 8
    });

    [ViewVariables, DataField]
    public DamageSpecifier HealPerConsume = new();

    /// STAGE PROGRESSION

    [ViewVariables, DataField, AutoNetworkedField]
    public SoundSpecifier LevelupSound = new SoundPathSpecifier("/Audio/SS220/DarkReaper/jnec_levelup.ogg", new()
    {
        MaxDistance = 8
    });

    /// <summary>
    /// DamageSpecifier for melee damage that Dark Reaper does at every stage.
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public List<Dictionary<string, FixedPoint2>> StageMeleeDamage = new()
    {
        // Stage 1
        new()
        {
            { "Slash", 12 },
            { "Piercing", 4 }
        },

        // Stage 2
        new()
        {
            { "Slash", 16 },
            { "Piercing", 8 }
        },

        // Stage 3
        new()
        {
            { "Slash", 20 },
            { "Piercing", 16 }
        }
    };

    /// <summary>
    /// DamageSpecifier for melee damage that Dark Reaper does at every stage.
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public List<DamageModifierSet> StageDamageResists = new()
    {
        // Stage 1
        new()
        {
            Coefficients = new()
            {
                {"Radiation", 0}
            }
        },

        // Stage 2
        new()
        {
            Coefficients = new()
            {
                {"Radiation", 0}
            }
        },

        // Stage 3
        new()
        {
            Coefficients = new()
            {
                {"Piercing", 0.5f},
                {"Slash", 0.5f},
                {"Blunt", 0.5f},
                {"Heat", 0.5f},
                {"Cold", 0.25f},
                {"Shock", 0.25f},
                {"Cellular", 0},
                {"Radiation", 0}
            }
        }
    };

    public List<int> ConsumedPerStage = new()
    {
        // stage 1 is free (initial)
        3,
        8
    };

    /// ABILITIES ///
    [DataField]
    public EntProtoId RoflAction = "ActionDarkReaperRofl";
    [DataField]
    public EntProtoId StunAction = "ActionDarkReaperStun";
    [DataField]
    public EntProtoId ConsumeAction = "ActionDarkReaperConsume";
    [DataField]
    public EntProtoId MaterializeAction = "ActionDarkReaperMaterialize";

    [DataField, AutoNetworkedField]
    public EntityUid? RoflActionEntity;
    [DataField, AutoNetworkedField]
    public EntityUid? StunActionEntity;
    [DataField, AutoNetworkedField]
    public EntityUid? ConsumeActionEntity;
    [DataField, AutoNetworkedField]
    public EntityUid? MaterializeActionEntity;

    // ABILITY STATES ///
    [ViewVariables, AutoNetworkedField]
    public TimeSpan? StunScreamStart;

    [ViewVariables]
    public EntityUid? ActivePortal;

    [ViewVariables, NonSerialized]
    public IPlayingAudioStream? PlayingPortalAudio;

    [ViewVariables, NonSerialized]
    public IPlayingAudioStream? ConsoomAudio;

    [ViewVariables]
    public TimeSpan? MaterializedStart;
}

[Serializable, NetSerializable]
public enum DarkReaperVisual
{
    Stage,
    PhysicalForm,
    StunEffect,
    GhostCooldown,
}
