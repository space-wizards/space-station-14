using System.Text.Json.Serialization;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Chemistry.Reagent
{
    [Prototype("reagent")]
    [DataDefinition]
    public sealed class ReagentPrototype : IPrototype, IInheritingPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("name", required: true)]
        private string Name { get; } = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedName => Loc.GetString(Name);

        [DataField("group")]
        public string Group { get; } = "Unknown";

        [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<ReagentPrototype>))]
        public string[]? Parents { get; private set; }

        [NeverPushInheritance]
        [AbstractDataFieldAttribute]
        public bool Abstract { get; private set; }

        [DataField("desc", required: true)]
        private string Description { get; } = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedDescription => Loc.GetString(Description);

        [DataField("physicalDesc", required: true)]
        private string PhysicalDescription { get; } = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedPhysicalDescription => Loc.GetString(PhysicalDescription);

        [DataField("flavor")]
        public string Flavor { get; } = default!;

        [DataField("color")]
        public Color SubstanceColor { get; } = Color.White;

        /// <summary>
        ///     The specific heat of the reagent.
        ///     How much energy it takes to heat one unit of this reagent by one Kelvin.
        /// </summary>
        [DataField("specificHeat")]
        public float SpecificHeat { get; } = 1.0f;

        [DataField("boilingPoint")]
        public float? BoilingPoint { get; }

        [DataField("meltingPoint")]
        public float? MeltingPoint { get; }

        [DataField("spritePath")]
        public string SpriteReplacementPath { get; } = string.Empty;

        [DataField("metabolisms", serverOnly: true, customTypeSerializer: typeof(PrototypeIdDictionarySerializer<ReagentEffectsEntry, MetabolismGroupPrototype>))]
        public Dictionary<string, ReagentEffectsEntry>? Metabolisms = null;

        [DataField("reactiveEffects", serverOnly: true, customTypeSerializer: typeof(PrototypeIdDictionarySerializer<ReactiveReagentEffectEntry, ReactiveGroupPrototype>))]
        public Dictionary<string, ReactiveReagentEffectEntry>? ReactiveEffects = null;

        [DataField("tileReactions", serverOnly: true)]
        public readonly List<ITileReaction> TileReactions = new(0);

        [DataField("plantMetabolism", serverOnly: true)]
        public readonly List<ReagentEffect> PlantMetabolisms = new(0);

        [DataField("pricePerUnit")]
        public float PricePerUnit { get; }

        /// <summary>
        /// If the substance color is too dark we user a lighter version to make the text color readable when the user examines a solution.
        /// </summary>
        public Color GetSubstanceTextColor()
        {
            var highestValue = MathF.Max(SubstanceColor.R, MathF.Max(SubstanceColor.G, SubstanceColor.B));
            var difference = 0.5f - highestValue;

            if (difference > 0f)
            {
                return new Color(SubstanceColor.R + difference,
                                SubstanceColor.G + difference,
                                SubstanceColor.B + difference);
            }

            return SubstanceColor;
        }

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

        public void ReactionPlant(EntityUid? plantHolder, Solution.ReagentQuantity amount, Solution solution)
        {
            if (plantHolder == null)
                return;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var random = IoCManager.Resolve<IRobustRandom>();
            var args = new ReagentEffectArgs(plantHolder.Value, null, solution, this, amount.Quantity, entMan, null, null);
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

    [DataDefinition]
    public sealed class ReagentEffectsEntry
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
    }

    [DataDefinition]
    public sealed class ReactiveReagentEffectEntry
    {
        [DataField("methods", required: true)]
        public HashSet<ReactionMethod> Methods = default!;

        [DataField("effects", required: true)]
        public ReagentEffect[] Effects = default!;
    }
}
