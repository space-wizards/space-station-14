using System.Collections.Frozen;
using System.Linq;
using Content.Shared.FixedPoint;
using System.Text.Json.Serialization;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Contraband;
using Content.Shared.EntityEffects;
using Content.Shared.Localizations;
using Content.Shared.Nutrition;
using Content.Shared.Roles;
using Content.Shared.Slippery;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Reagent
{
    [Prototype]
    [DataDefinition]
    public sealed partial class ReagentPrototype : IPrototype, IInheritingPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField(required: true)]
        private LocId Name { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedName => Loc.GetString(Name);

        [DataField]
        public string Group { get; private set; } = "Unknown";

        [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ReagentPrototype>))]
        public string[]? Parents { get; private set; }

        [NeverPushInheritance]
        [AbstractDataField]
        public bool Abstract { get; private set; }

        [DataField("desc", required: true)]
        private LocId Description { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedDescription => Loc.GetString(Description);

        [DataField("physicalDesc", required: true)]
        private LocId PhysicalDescription { get; set; } = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedPhysicalDescription => Loc.GetString(PhysicalDescription);

        /// <summary>
        ///     The degree of contraband severity this reagent is considered to have.
        ///     If AllowedDepartments or AllowedJobs are set, they take precedent and override this value.
        /// </summary>
        [DataField]
        public ProtoId<ContrabandSeverityPrototype>? ContrabandSeverity = null;

        /// <summary>
        ///     Which departments is this reagent restricted to, if any?
        /// </summary>
        [DataField]
        public HashSet<ProtoId<DepartmentPrototype>> AllowedDepartments = new();

        /// <summary>
        ///     Which jobs is this reagent restricted to, if any?
        /// </summary>
        [DataField]
        public HashSet<ProtoId<JobPrototype>> AllowedJobs = new();

        /// <summary>
        ///     Is this reagent recognizable to the average spaceman (water, welding fuel, ketchup, etc)?
        /// </summary>
        [DataField]
        public bool Recognizable;

        /// <summary>
        /// Whether this reagent stands out (blood, slime).
        /// </summary>
        [DataField]
        public bool Standsout;

        [DataField]
        public ProtoId<FlavorPrototype>? Flavor;

        /// <summary>
        /// There must be at least this much quantity in a solution to be tasted.
        /// </summary>
        [DataField]
        public FixedPoint2 FlavorMinimum = FixedPoint2.New(0.1f);

        [DataField("color")]
        public Color SubstanceColor { get; private set; } = Color.White;

        /// <summary>
        ///     The specific heat of the reagent.
        ///     How much energy it takes to heat one unit of this reagent by one Kelvin.
        /// </summary>
        [DataField]
        public float SpecificHeat { get; private set; } = 1.0f;

        [DataField]
        public float? BoilingPoint { get; private set; }

        [DataField]
        public float? MeltingPoint { get; private set; }

        [DataField]
        public SpriteSpecifier? MetamorphicSprite { get; private set; } = null;

        [DataField]
        public int MetamorphicMaxFillLevels { get; private set; } = 0;

        [DataField]
        public string? MetamorphicFillBaseName { get; private set; } = null;

        [DataField]
        public bool MetamorphicChangeColor { get; private set; } = true;

        /// <summary>
        /// If not null, makes something slippery. Also defines slippery interactions like stun time and launch mult.
        /// </summary>
        [DataField]
        public SlipperyEffectEntry? SlipData;

        /// <summary>
        /// The speed at which the reagent evaporates over time.
        /// </summary>
        [DataField]
        public FixedPoint2 EvaporationSpeed = FixedPoint2.Zero;

        /// <summary>
        /// If this reagent can be used to mop up other reagents.
        /// </summary>
        [DataField]
        public bool Absorbent = false;

        /// <summary>
        /// How easily this reagent becomes fizzy when aggitated.
        /// 0 - completely flat, 1 - fizzes up when nudged.
        /// </summary>
        [DataField]
        public float Fizziness;

        /// <summary>
        /// How much reagent slows entities down if it's part of a puddle.
        /// 0 - no slowdown; 1 - can't move.
        /// </summary>
        [DataField]
        public float Viscosity;

        /// <summary>
        /// Linear Friction Multiplier for a reagent
        /// 0 - frictionless, 1 - no effect on friction
        /// </summary>
        [DataField]
        public float Friction = 1.0f;

        /// <summary>
        /// Should this reagent work on the dead?
        /// </summary>
        [DataField]
        public bool WorksOnTheDead;

        [DataField]
        public FrozenDictionary<ProtoId<MetabolismGroupPrototype>, ReagentEffectsEntry>? Metabolisms;

        [DataField]
        public Dictionary<ProtoId<ReactiveGroupPrototype>, ReactiveReagentEffectEntry>? ReactiveEffects;

        [DataField(serverOnly: true)]
        public List<ITileReaction> TileReactions = new(0);

        [DataField("plantMetabolism")]
        public List<EntityEffect> PlantMetabolisms = new(0);

        [DataField]
        public float PricePerUnit;

        [DataField]
        public SoundSpecifier FootstepSound = new SoundCollectionSpecifier("FootstepPuddle");

        // TODO: Reaction tile doesn't work properly and destroys reagents way too quickly
        public FixedPoint2 ReactionTile(TileRef tile, FixedPoint2 reactVolume, IEntityManager entityManager, List<ReagentData>? data)
        {
            var removed = FixedPoint2.Zero;

            if (tile.Tile.IsEmpty)
                return removed;

            foreach (var reaction in TileReactions)
            {
                removed += reaction.TileReact(tile, this, reactVolume - removed, entityManager, data);

                if (removed > reactVolume)
                    throw new Exception("Removed more than we have!");

                if (removed == reactVolume)
                    break;
            }

            return removed;
        }

        public IEnumerable<string> GuidebookReagentEffectsDescription(IPrototypeManager prototype, IEntitySystemManager entSys, IEnumerable<EntityEffect> effects, FixedPoint2 metabolism)
        {
            return effects.Select(x => GuidebookReagentEffectDescription(prototype, entSys, x, metabolism))
                .Where(x => x is not null)
                .Select(x => x!)
                .ToArray();
        }

        public string? GuidebookReagentEffectDescription(IPrototypeManager prototype, IEntitySystemManager entSys, EntityEffect effect, FixedPoint2 metabolism)
        {
            if (effect.EntityEffectGuidebookText(prototype, entSys) is not { } description)
                return null;

            var quantity = (double)(effect.MinScale * metabolism);

            return Loc.GetString(
                "guidebook-reagent-effect-description",
                ("reagent", LocalizedName),
                ("quantity", quantity),
                ("effect", description),
                ("chance", effect.Probability),
                ("conditionCount", effect.Conditions?.Length ?? 0),
                ("conditions",
                    ContentLocalizationManager.FormatList(
                        effect.Conditions?.Select(x => x.EntityConditionGuidebookText(prototype)).ToList() ?? new List<string>()
                    )));
        }
    }

    [Serializable, NetSerializable]
    public struct ReagentGuideEntry
    {
        public string ReagentPrototype;

        // TODO: Kill Metabolism groups!
        public Dictionary<ProtoId<MetabolismGroupPrototype>, ReagentEffectsGuideEntry>? GuideEntries;

        public List<string>? PlantMetabolisms = null;

        public ReagentGuideEntry(ReagentPrototype proto, IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            ReagentPrototype = proto.ID;
            GuideEntries = proto.Metabolisms?
                .Select(x => (x.Key, x.Value.MakeGuideEntry(prototype, entSys, proto)))
                .ToDictionary(x => x.Key, x => x.Item2);
            if (proto.PlantMetabolisms.Count > 0)
            {
                PlantMetabolisms =
                    new List<string>(proto.GuidebookReagentEffectsDescription(prototype, entSys, proto.PlantMetabolisms, FixedPoint2.New(1f)));
            }
        }
    }


    [DataDefinition]
    public sealed partial class ReagentEffectsEntry
    {
        /// <summary>
        ///     Amount of reagent to metabolize, per metabolism cycle.
        /// </summary>
        [JsonPropertyName("rate")]
        [DataField("metabolismRate")]
        public FixedPoint2 MetabolismRate = FixedPoint2.New(0.5f);

        /// <summary>
        ///     A list of effects to apply when these reagents are metabolized.
        /// </summary>
        [JsonPropertyName("effects")]
        [DataField("effects", required: true)]
        public EntityEffect[] Effects = default!;

        public string EntityEffectFormat => "guidebook-reagent-effect-description";

        public ReagentEffectsGuideEntry MakeGuideEntry(IPrototypeManager prototype, IEntitySystemManager entSys, ReagentPrototype proto)
        {
            return new ReagentEffectsGuideEntry(MetabolismRate, proto.GuidebookReagentEffectsDescription(prototype, entSys, Effects, MetabolismRate).ToArray());
        }
    }

    [Serializable, NetSerializable]
    public struct ReagentEffectsGuideEntry
    {
        public FixedPoint2 MetabolismRate;

        public string[] EffectDescriptions;

        public ReagentEffectsGuideEntry(FixedPoint2 metabolismRate, string[] effectDescriptions)
        {
            MetabolismRate = metabolismRate;
            EffectDescriptions = effectDescriptions;
        }
    }

    [DataDefinition]
    public sealed partial class ReactiveReagentEffectEntry
    {
        [DataField("methods", required: true)]
        public HashSet<ReactionMethod> Methods = default!;

        [DataField("effects", required: true)]
        public EntityEffect[] Effects = default!;
    }
}
