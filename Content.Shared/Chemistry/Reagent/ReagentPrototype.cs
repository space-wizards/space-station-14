using System.Linq;
using System.Text.Json.Serialization;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Reagent
{
    [Prototype("reagent")]
    [DataDefinition]
    public sealed partial class ReagentPrototype : IPrototype, IInheritingPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("name", required: true)]
        private string Name { get; set; } = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedName => Loc.GetString(Name);

        [DataField("group")]
        public string Group { get; private set; } = "Unknown";

        [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<ReagentPrototype>))]
        public string[]? Parents { get; private set; }

        [NeverPushInheritance]
        [AbstractDataFieldAttribute]
        public bool Abstract { get; private set; }

        [DataField("desc", required: true)]
        private string Description { get; set; } = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedDescription => Loc.GetString(Description);

        [DataField("physicalDesc", required: true)]
        private string PhysicalDescription { get; set; } = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedPhysicalDescription => Loc.GetString(PhysicalDescription);

        /// <summary>
        ///     Is this reagent recognizable to the average spaceman (water, welding fuel, ketchup, etc)?
        /// </summary>
        [DataField("recognizable")]
        public bool Recognizable = false;

        [DataField("flavor", customTypeSerializer:typeof(PrototypeIdSerializer<FlavorPrototype>))]
        public string? Flavor;

        /// <summary>
        /// There must be at least this much quantity in a solution to be tasted.
        /// </summary>
        [DataField("flavorMinimum")]
        public FixedPoint2 FlavorMinimum = FixedPoint2.New(0.1f);

        [DataField("color")]
        public Color SubstanceColor { get; private set; } = Color.White;

        /// <summary>
        ///     The specific heat of the reagent.
        ///     How much energy it takes to heat one unit of this reagent by one Kelvin.
        /// </summary>
        [DataField("specificHeat")]
        public float SpecificHeat { get; private set; } = 1.0f;

        [DataField("boilingPoint")]
        public float? BoilingPoint { get; private set; }

        [DataField("meltingPoint")]
        public float? MeltingPoint { get; private set; }

        [DataField("metamorphicSprite")]
        public SpriteSpecifier? MetamorphicSprite { get; private set; } = null;

        /// <summary>
        /// If this reagent is part of a puddle is it slippery.
        /// </summary>
        [DataField("slippery")]
        public bool Slippery = false;

        /// <summary>
        /// How much reagent slows entities down if it's part of a puddle.
        /// 0 - no slowdown; 1 - can't move.
        /// </summary>
        [DataField("viscosity")]
        public float Viscosity = 0;

        [DataField("metabolisms", serverOnly: true, customTypeSerializer: typeof(PrototypeIdDictionarySerializer<ReagentEffectsEntry, MetabolismGroupPrototype>))]
        public Dictionary<string, ReagentEffectsEntry>? Metabolisms = null;

        [DataField("reactiveEffects", serverOnly: true, customTypeSerializer: typeof(PrototypeIdDictionarySerializer<ReactiveReagentEffectEntry, ReactiveGroupPrototype>))]
        public Dictionary<string, ReactiveReagentEffectEntry>? ReactiveEffects = null;

        [DataField("tileReactions", serverOnly: true)]
        public List<ITileReaction> TileReactions = new(0);

        [DataField("plantMetabolism", serverOnly: true)]
        public List<ReagentEffect> PlantMetabolisms = new(0);

        [DataField("pricePerUnit")] public float PricePerUnit;

        // TODO: Pick the highest reagent for sounds and add sticky to cola, juice, etc.
        [DataField("footstepSound")]
        public SoundSpecifier FootstepSound = new SoundCollectionSpecifier("FootstepWater");

        public FixedPoint2 ReactionTile(TileRef tile, FixedPoint2 reactVolume)
        {
            var removed = FixedPoint2.Zero;

            if (tile.Tile.IsEmpty)
                return removed;

            foreach (var reaction in TileReactions)
            {
                removed += reaction.TileReact(tile, this, reactVolume - removed);

                if (removed > reactVolume)
                    throw new Exception("Removed more than we have!");

                if (removed == reactVolume)
                    break;
            }

            return removed;
        }

        public void ReactionPlant(EntityUid? plantHolder, ReagentQuantity amount, Solution solution)
        {
            if (plantHolder == null)
                return;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var random = IoCManager.Resolve<IRobustRandom>();
            var args = new ReagentEffectArgs(plantHolder.Value, null, solution, this, amount.Quantity, entMan, null, 1f);
            foreach (var plantMetabolizable in PlantMetabolisms)
            {
                if (!plantMetabolizable.ShouldApply(args, random))
                    continue;

                if (plantMetabolizable.ShouldLog)
                {
                    var entity = args.SolutionEntity;
                    EntitySystem.Get<SharedAdminLogSystem>().Add(LogType.ReagentEffect, plantMetabolizable.LogImpact,
                        $"Plant metabolism effect {plantMetabolizable.GetType().Name:effect} of reagent {ID:reagent} applied on entity {entMan.ToPrettyString(entity):entity} at {entMan.GetComponent<TransformComponent>(entity).Coordinates:coordinates}");
                }

                plantMetabolizable.Effect(args);
            }
        }
    }

    [Serializable, NetSerializable]
    public struct ReagentGuideEntry
    {
        public string ReagentPrototype;

        public Dictionary<string, ReagentEffectsGuideEntry>? GuideEntries;

        public ReagentGuideEntry(ReagentPrototype proto, IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            ReagentPrototype = proto.ID;
            GuideEntries = proto.Metabolisms?
                .Select(x => (x.Key, x.Value.MakeGuideEntry(prototype, entSys)))
                .ToDictionary(x => x.Key, x => x.Item2);
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
        public ReagentEffect[] Effects = default!;

        public ReagentEffectsGuideEntry MakeGuideEntry(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            return new ReagentEffectsGuideEntry(MetabolismRate,
                Effects
                    .Select(x => x.GuidebookEffectDescription(prototype, entSys)) // hate.
                    .Where(x => x is not null)
                    .Select(x => x!)
                    .ToArray());
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
        public ReagentEffect[] Effects = default!;
    }
}
