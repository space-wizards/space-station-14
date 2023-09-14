using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Access(typeof(BloodstreamSystem), (typeof(ChemistrySystem)))]
    public sealed partial class BloodstreamComponent : Component
    {
        public static string DefaultChemicalsSolutionName = "chemicals";
        public static string DefaultBloodSolutionName = "bloodstream";
        public static string DefaultBloodTemporarySolutionName = "bloodstreamTemporary";

        public float AccumulatedFrametime = 0.0f;

        /// <summary>
        ///     How much is this entity currently bleeding?
        ///     Higher numbers mean more blood lost every tick.
        ///
        ///     Goes down slowly over time, and items like bandages
        ///     or clotting reagents can lower bleeding.
        /// </summary>
        /// <remarks>
        ///     This generally corresponds to an amount of damage and can't go above 100.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public float BleedAmount;

        /// <summary>
        ///     How much should bleeding should be reduced every update interval?
        /// </summary>
        [DataField("bleedReductionAmount")]
        public float BleedReductionAmount = 1.0f;

        /// <summary>
        ///     How high can <see cref="BleedAmount"/> go?
        /// </summary>
        [DataField("maxBleedAmount")]
        public float MaxBleedAmount = 10.0f;

        /// <summary>
        ///     What percentage of current blood is necessary to avoid dealing blood loss damage?
        /// </summary>
        [DataField("bloodlossThreshold")]
        public float BloodlossThreshold = 0.9f;

        /// <summary>
        ///     The base bloodloss damage to be incurred if below <see cref="BloodlossThreshold"/>
        ///     The default values are defined per mob/species in YML.
        /// </summary>
        [DataField("bloodlossDamage", required: true)]
        public DamageSpecifier BloodlossDamage = new();

        /// <summary>
        ///     The base bloodloss damage to be healed if above <see cref="BloodlossThreshold"/>
        ///     The default values are defined per mob/species in YML.
        /// </summary>
        [DataField("bloodlossHealDamage", required: true)]
        public DamageSpecifier BloodlossHealDamage = new();

        /// <summary>
        ///     How frequently should this bloodstream update, in seconds?
        /// </summary>
        [DataField("updateInterval")]
        public float UpdateInterval = 3.0f;

        // TODO shouldn't be hardcoded, should just use some organ simulation like bone marrow or smth.
        /// <summary>
        ///     How much reagent of blood should be restored each update interval?
        /// </summary>
        [DataField("bloodRefreshAmount")]
        public float BloodRefreshAmount = 1.0f;

        /// <summary>
        ///     How much blood needs to be in the temporary solution in order to create a puddle?
        /// </summary>
        [DataField("bleedPuddleThreshold")]
        public FixedPoint2 BleedPuddleThreshold = 1.0f;

        /// <summary>
        ///     A modifier set prototype ID corresponding to how damage should be modified
        ///     before taking it into account for bloodloss.
        /// </summary>
        /// <remarks>
        ///     For example, piercing damage is increased while poison damage is nullified entirely.
        /// </remarks>
        [DataField("damageBleedModifiers", customTypeSerializer:typeof(PrototypeIdSerializer<DamageModifierSetPrototype>))]
        public string DamageBleedModifiers = "BloodlossHuman";

        /// <summary>
        ///     The sound to be played when a weapon instantly deals blood loss damage.
        /// </summary>
        [DataField("instantBloodSound")]
        public SoundSpecifier InstantBloodSound = new SoundCollectionSpecifier("blood");

        /// <summary>
        ///     The sound to be played when some damage actually heals bleeding rather than starting it.
        /// </summary>
        [DataField("bloodHealedSound")]
        public SoundSpecifier BloodHealedSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

        // TODO probably damage bleed thresholds.

        /// <summary>
        ///     Max volume of internal chemical solution storage
        /// </summary>
        [DataField("chemicalMaxVolume")]
        public FixedPoint2 ChemicalMaxVolume = FixedPoint2.New(250);

        /// <summary>
        ///     Max volume of internal blood storage,
        ///     and starting level of blood.
        /// </summary>
        [DataField("bloodMaxVolume")]
        public FixedPoint2 BloodMaxVolume = FixedPoint2.New(300);

        /// <summary>
        ///     Which reagent is considered this entities 'blood'?
        /// </summary>
        /// <remarks>
        ///     Slime-people might use slime as their blood or something like that.
        /// </remarks>
        [DataField("bloodReagent")]
        public string BloodReagent = "Blood";

        /// <summary>
        ///     Internal solution for reagent storage
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Access(typeof(BloodstreamSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public Solution ChemicalSolution = default!;

        /// <summary>
        ///     Internal solution for blood storage
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Solution BloodSolution = default!;

        /// <summary>
        ///     Temporary blood solution.
        ///     When blood is lost, it goes to this solution, and when this
        ///     solution hits a certain cap, the blood is actually spilled as a puddle.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Solution BloodTemporarySolution = default!;

        /// <summary>
        /// Variable that stores the amount of status time added by having a low blood level.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float StatusTime;
    }
}
