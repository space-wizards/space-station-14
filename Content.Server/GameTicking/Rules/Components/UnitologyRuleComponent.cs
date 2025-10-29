// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Prototypes;
using Robust.Shared.Map;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component for the UnitologyRuleSystem that stores info about winning/losing, player counts required for starting, as well as prototypes for unitologs and their gear.
/// </summary>
[RegisterComponent, Access(typeof(UnitologyRuleSystem))]
public sealed partial class UnitologyRuleComponent : Component
{
    /// <summary>
    /// Sound that plays when you are chosen as unitologs.
    /// </summary>
    [DataField]
    public SoundSpecifier UniStartSound = new SoundPathSpecifier("/Audio/_DeadSpace/Necromorfs/unitolog_start.ogg");

    /// <summary>
    /// Sound that plays before convergence stage.
    /// </summary>
    [DataField]
    public SoundSpecifier ConvergenceMusic = new SoundCollectionSpecifier("ConvergenceMusic");

    /// <summary>
    ///     Check if convergence music starts playing so we don't do it again
    /// </summary>
    public bool PlayedConvergenceSong = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsEndConvergence = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsStageObelisk = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsStageConvergence = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsObeliskWarningSend = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsUniWarningSend = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsObeliskArrival = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsTransformationEnd = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool ThisExplosionMade = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool CburnSended = false;

    [DataField("obeliskPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ObeliskPrototype = "StructureObelisk";

    [DataField("blackObeliskPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BlackObeliskPrototype = "StructureBlackObelisk";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Obelisk;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float CburnDuration = 5;

    [DataField]
    public TimeSpan TimeUntilCburn = TimeSpan.Zero;

    [DataField]
    public TimeSpan NextStageTime;

    [DataField]
    public TimeSpan StageObeliskDuration = TimeSpan.FromMinutes(20);

    [DataField]
    public TimeSpan StageConvergenceDuration = TimeSpan.FromMinutes(1);

    [DataField]
    public TimeSpan TimeUntilArrivalObelisk = TimeSpan.Zero;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtilStopTransformations = TimeSpan.Zero;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DamageTick;

    [DataField]
    public float DurationArrivalObelisk = 40f;

    [DataField]
    public float TimeUntilUniWarning = 20f;

    [DataField]
    public float TimeUntilObeliskWarning = 10f;

    [DataField]
    public float TimeAfterTheExplosion = 2f;

    [DataField]
    public string TypeId = "MicroBomb";

    [DataField]
    public float TotalIntensity = 300f;

    [DataField]
    public float MaxTileIntensity = 20f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float DurationTransformations = 10f;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Blunt", 3 },
            { "Slash", 2 },
            { "Piercing", 4 },
            {"Structural", 10}
        }
    };

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier TransformationsSound = new SoundCollectionSpecifier("DevaEat");

    [DataField("afterGibNecroPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<NecromorfPrototype>))]
    public string AfterGibNecroPrototype = "NecroCorpseCollector";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates? ObeliskCoords = null;
}

[ByRefEvent]
public readonly record struct StageConvergenceEvent();

[ByRefEvent]
public readonly record struct EndStageConvergenceEvent();

[ByRefEvent]
public readonly record struct StageObeliskEvent(EntityUid Obelisk);
