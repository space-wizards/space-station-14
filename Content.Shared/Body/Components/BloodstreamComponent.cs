using Content.Shared.Alert;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Body.Components;

/// <summary>
/// Gives an entity a bloodstream.
/// </summary>
[RegisterComponent, NetworkedComponent,]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(SharedBloodstreamSystem))]
public sealed partial class BloodstreamComponent : Component
{
    public const string DefaultBloodSolutionName = "bloodstream";
    public const string DefaultBloodTemporarySolutionName = "bloodstreamTemporary";

    /// <summary>
    /// The next time that blood level will be updated and bloodloss damage dealt.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The interval at which this component updates.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Multiplier applied to <see cref="UpdateInterval"/> for adjusting based on metabolic rate multiplier.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// Adjusted update interval based off of the multiplier value.
    /// </summary>
    [ViewVariables]
    public TimeSpan AdjustedUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    /// <summary>
    /// How much is this entity currently bleeding?
    /// Higher numbers mean more blood lost every tick.
    ///
    /// Goes down slowly over time, and items like bandages
    /// or clotting reagents can lower bleeding.
    /// </summary>
    /// <remarks>
    /// This generally corresponds to an amount of damage and can't go above 100.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public float BleedAmount;

    /// <summary>
    /// How much should bleeding be reduced every update interval?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BleedReductionAmount = 0.33f;

    /// <summary>
    /// How high can <see cref="BleedAmount"/> go?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxBleedAmount = 10.0f;

    /// <summary>
    /// What percentage of current blood is necessary to avoid dealing blood loss damage?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BloodlossThreshold = 0.9f;

    /// <summary>
    /// The base bloodloss damage to be incurred if below <see cref="BloodlossThreshold"/>
    /// The default values are defined per mob/species in YML.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier BloodlossDamage = new();

    /// <summary>
    /// The base bloodloss damage to be healed if above <see cref="BloodlossThreshold"/>
    /// The default values are defined per mob/species in YML.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier BloodlossHealDamage = new();

    // TODO shouldn't be hardcoded, should just use some organ simulation like bone marrow or smth.
    /// <summary>
    /// How much reagent of blood should be restored each update interval?
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodRefreshAmount = 1.0f;

    /// <summary>
    /// How much blood needs to be in the temporary solution in order to create a puddle?
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BleedPuddleThreshold = 1.0f;

    /// <summary>
    /// A modifier set prototype ID corresponding to how damage should be modified
    /// before taking it into account for bloodloss.
    /// </summary>
    /// <remarks>
    /// For example, piercing damage is increased while poison damage is nullified entirely.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public ProtoId<DamageModifierSetPrototype> DamageBleedModifiers = "BloodlossHuman";

    /// <summary>
    /// The sound to be played when a weapon instantly deals blood loss damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier InstantBloodSound = new SoundCollectionSpecifier("blood");

    /// <summary>
    /// The sound to be played when some damage actually heals bleeding rather than starting it.
    /// </summary>
    [DataField]
    public SoundSpecifier BloodHealedSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    /// <summary>
    /// The minimum amount damage reduction needed to play the healing sound/popup.
    /// This prevents tiny amounts of heat damage from spamming the sound, e.g. spacing.
    /// </summary>
    [DataField]
    public float BloodHealedSoundThreshold = -0.1f;

    // TODO probably damage bleed thresholds.

    /// <summary>
    /// Modifier applied to <see cref="BloodstreamComponent.BloodReferenceSolution.Volume"/> to determine maximum volume for bloodstream.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxVolumeModifier = 2f;

    /// <summary>
    /// Defines which reagents are considered as 'blood' and how much of it is normal.
    /// </summary>
    /// <remarks>
    /// Slime-people might use slime as their blood or something like that.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public Solution BloodReferenceSolution = new([new("Blood", 300)]);

    /// <summary>
    /// Caches the blood data of an entity.
    /// This is modified by DNA on init so it's not savable.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<ReagentData>? BloodData;

    /// <summary>
    /// Name/Key that <see cref="BloodSolution"/> is indexed by.
    /// </summary>
    [DataField]
    public string BloodSolutionName = DefaultBloodSolutionName;

    /// <summary>
    /// Name/Key that <see cref="TemporarySolution"/> is indexed by.
    /// </summary>
    [DataField]
    public string BloodTemporarySolutionName = DefaultBloodTemporarySolutionName;

    /// <summary>
    /// Internal solution for blood storage
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? BloodSolution;

    /// <summary>
    /// Temporary blood solution.
    /// When blood is lost, it goes to this solution, and when this
    /// solution hits a certain cap, the blood is actually spilled as a puddle.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? TemporarySolution;

    /// <summary>
    /// Alert to show when bleeding.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BleedingAlert = "Bleed";
}
