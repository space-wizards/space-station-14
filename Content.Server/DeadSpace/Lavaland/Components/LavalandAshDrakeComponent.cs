using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Lavaland.Components;

[RegisterComponent]
public sealed partial class LavalandAshDrakeComponent : Component
{
    [DataField]
    public TimeSpan RangedCooldown = TimeSpan.FromSeconds(3.2);

    [DataField]
    public TimeSpan ForcePressureAfter = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan TargetSwitchCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan TargetPressureMemory = TimeSpan.FromSeconds(30);

    [DataField]
    public TimeSpan FireWallStepDelay = TimeSpan.FromSeconds(0.06);

    [DataField]
    public TimeSpan FireRainDelay = TimeSpan.FromSeconds(0.9);

    [DataField]
    public TimeSpan SwoopWindup = TimeSpan.FromSeconds(0.6);

    [DataField]
    public TimeSpan SwoopStepDelay = TimeSpan.FromSeconds(0.085);

    [DataField]
    public TimeSpan SwoopRecover = TimeSpan.FromSeconds(0.35);

    [DataField]
    public TimeSpan ChainedSwoopDelay = TimeSpan.FromSeconds(0.28);

    [DataField]
    public int FireWallRange = 10;

    [DataField]
    public int FireRainRadius = 9;

    [DataField]
    public float FireRainTileChance = 0.14f;

    [DataField]
    public int FireRainMaxTiles = 24;

    [DataField]
    public int SwoopSteps = 36;

    [DataField]
    public int TripleSwoopSteps = 28;

    [DataField]
    public int SwoopFireRainMaxTiles = 12;

    [DataField]
    public int MaxPendingTiles = 160;

    [DataField]
    public float FireStacks = 2.5f;

    [DataField]
    public float SwoopThrowSpeed = 7.5f;

    [DataField]
    public string FirePrototype = "LavalandAshDrakeFire";

    [DataField]
    public string FireRainTargetPrototype = "LavalandAshDrakeTarget";

    [DataField]
    public string FireRainFireballPrototype = "LavalandAshDrakeFireball";

    [DataField]
    public string LandingPrototype = "LavalandAshDrakeLanding";

    [DataField]
    public DamageSpecifier FireWallDamage = new()
    {
        DamageDict = new()
        {
            { "Heat", FixedPoint2.New(20) },
        },
    };

    [DataField]
    public DamageSpecifier FireRainDamage = new()
    {
        DamageDict = new()
        {
            { "Heat", FixedPoint2.New(40) },
        },
    };

    [DataField]
    public DamageSpecifier SwoopDamage = new()
    {
        DamageDict = new()
        {
            { "Blunt", FixedPoint2.New(45) },
            { "Heat", FixedPoint2.New(30) },
        },
    };

    [DataField]
    public SoundSpecifier FireSound = new SoundPathSpecifier("/Audio/_DeadSpace/Lavaland/AshDrake/fireball.ogg");

    [DataField]
    public SoundSpecifier FireRainSound = new SoundPathSpecifier("/Audio/_DeadSpace/Lavaland/AshDrake/fleshtostone.ogg");

    [DataField]
    public SoundSpecifier ImpactSound = new SoundPathSpecifier("/Audio/_DeadSpace/Lavaland/AshDrake/meteorimpact.ogg");

    [DataField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/_DeadSpace/Lavaland/AshDrake/sear.ogg");

    [DataField]
    public float CageAttackChance = 0.22f;

    [DataField]
    public TimeSpan CageDuration = TimeSpan.FromSeconds(14);

    [DataField]
    public TimeSpan CageTargetInterval = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan CageDamageInterval = TimeSpan.FromSeconds(0.75);

    [DataField]
    public int CageHalfSize = 3;

    [DataField]
    public int CagePhaseCount = 3;

    [DataField]
    public string CageBorderFirePrototype = "LavalandAshDrakeCageBarrier";

    [DataField]
    public string CageTargetPrototype = "LavalandAshDrakeCageTarget";

    [DataField]
    public string CageFirePrototype = "LavalandAshDrakeCageFire";

    [DataField]
    public DamageSpecifier CageInteriorFireDamage = new()
    {
        DamageDict = new()
        {
            { "Heat", FixedPoint2.New(35) },
        },
    };

    [DataField]
    public float WhelpPhaseHealthFraction = 0.5f;

    [DataField]
    public float LavaPhaseHealthFraction = 0.35f;

    [DataField]
    public string WhelpPrototype = "MobLavalandAshWhelp";

    [DataField]
    public string WhelpTargetPrototype = "LavalandAshDrakeWhelpTarget";

    [DataField]
    public float WhelpWaveChance = 0.6f;

    [DataField]
    public TimeSpan WhelpWaveCooldown = TimeSpan.FromSeconds(8);
    [DataField]
    public TimeSpan WhelpAttackDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public int WhelpSpawnDistance = 3;

    [DataField]
    public int WhelpFireRange = 6;

    [DataField]
    public int ArenaLavaDepth = 7;

    [DataField]
    public string CornerLavaPrototype = "FloorLavaEntity";
    [ViewVariables]
    public TimeSpan NextAttack;

    [ViewVariables]
    public TimeSpan BusyUntil;

    [ViewVariables]
    public TimeSpan LastPressureAt;

    [ViewVariables]
    public string LastAttackKind = string.Empty;

    [ViewVariables]
    public EntityUid? CurrentPrimaryTarget;

    [ViewVariables]
    public TimeSpan LastTargetSwitchAt;

    [ViewVariables]
    public readonly Dictionary<EntityUid, TimeSpan> LastPressureByTarget = new();

    [ViewVariables]
    public bool Swooping;

    [ViewVariables]
    public bool SwoopInvulnerable;

    [ViewVariables]
    public EntityUid? SwoopTarget;

    [ViewVariables]
    public int SwoopRemainingSteps;

    [ViewVariables]
    public bool SwoopDropsFireRain;

    [ViewVariables]
    public int SwoopFireRainTilesQueued;

    [ViewVariables]
    public TimeSpan NextSwoopStep;

    [ViewVariables]
    public TimeSpan SwoopImpactAt;

    [ViewVariables]
    public int PendingSwoops;

    [ViewVariables]
    public int PendingSwoopSteps;

    [ViewVariables]
    public TimeSpan NextQueuedSwoop;

    [ViewVariables]
    public readonly List<LavalandAshDrakePendingTile> PendingTiles = new();

    [ViewVariables]
    public bool CageActive;

    [ViewVariables]
    public int CagePhase;

    [ViewVariables]
    public Vector2i CageCenter;

    [ViewVariables]
    public TimeSpan CageEndAt;

    [ViewVariables]
    public TimeSpan NextCageTargetAt;

    [ViewVariables]
    public TimeSpan NextCageFillAt;

    [ViewVariables]
    public TimeSpan NextCageDamageTick;

    [ViewVariables]
    public Vector2i? CurrentCageTargetTile;

    [ViewVariables]
    public Vector2i? CageSafeTile;

    [ViewVariables]
    public EntityUid? CageTargetEntity;

    [ViewVariables]
    public readonly List<EntityUid> CageBorderEntities = new();

    [ViewVariables]
    public readonly List<EntityUid> CageInteriorEntities = new();

    [ViewVariables]
    public TimeSpan NextWhelpWave;

    [ViewVariables]
    public bool WhelpPhaseTriggered;

    [ViewVariables]
    public bool LavaPhaseTriggered;

    [ViewVariables]
    public readonly List<LavalandAshDrakeWhelpAttack> WhelpAttacks = new();

    [ViewVariables]
    public readonly List<EntityUid> PhaseEntities = new();
}

public sealed class LavalandAshDrakeWhelpAttack
{
    public EntityUid Whelp;
    public EntityUid Grid;
    public TimeSpan AttackAt;
    public readonly HashSet<Vector2i> Tiles = new();
    public readonly List<EntityUid> Telegraphs = new();
}
public sealed class LavalandAshDrakePendingTile
{
    public EntityUid Grid;
    public Vector2i Tile;
    public TimeSpan DetonateAt;
    public DamageSpecifier Damage = new();
    public bool Ignite;
    public bool PlayImpactSound;
    public string EffectPrototype = string.Empty;
}

[RegisterComponent]
public sealed partial class LavalandAshDrakeFireComponent : Component
{
    [DataField]
    public TimeSpan InitialDelay = TimeSpan.Zero;

    [DataField]
    public TimeSpan DamageInterval = TimeSpan.FromSeconds(0.3);

    [DataField]
    public float FireStacks = 2.5f;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Heat", FixedPoint2.New(10) },
        },
    };

    [ViewVariables]
    public TimeSpan SpawnedAt;

    [ViewVariables]
    public readonly Dictionary<EntityUid, TimeSpan> NextDamageByEntity = new();
}
