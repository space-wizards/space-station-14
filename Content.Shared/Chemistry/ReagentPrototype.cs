#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry
{
    [Prototype("reagent")]
    [DataDefinition]
    public class ReagentPrototype : IPrototype
    {
        [DataField("metabolism", serverOnly: true)]
        private readonly List<IMetabolizable> _metabolism = new() {new DefaultMetabolizable()};

        [DataField("tileReactions", serverOnly: true)]
        private readonly List<ITileReaction> _tileReactions = new(0);

        [DataField("plantMetabolism", serverOnly: true)]
        private readonly List<IPlantMetabolizable> _plantMetabolism = new(0);

        [DataField("customPlantMetabolism")]
        private readonly float _customPlantMetabolism = 1f;

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        [DataField("desc")]
        public string Description { get; } = string.Empty;

        [DataField("physicalDesc")]
        public string PhysicalDescription { get; } = string.Empty;

        [DataField("color")]
        public Color SubstanceColor { get; } = Color.White;

        [DataField("toxin")]
        public bool Toxin { get; }

        [DataField("boozePower")]
        public int BoozePower { get; }

        [DataField("boilingPoint")]
        public float? BoilingPoint { get; }

        [DataField("meltingPoint")]
        public float? MeltingPoint { get; }

        [DataField("spritePath")]
        public string SpriteReplacementPath { get; } = string.Empty;

        //List of metabolism effects this reagent has, should really only be used server-side.
        public IReadOnlyList<IMetabolizable> Metabolism => _metabolism;
        public IReadOnlyList<ITileReaction> TileReactions => _tileReactions;
        public IReadOnlyList<IPlantMetabolizable> PlantMetabolism => _plantMetabolism;

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

        public ReagentUnit ReactionTile(TileRef tile, ReagentUnit reactVolume)
        {
            var removed = ReagentUnit.Zero;

            if (tile.Tile.IsEmpty)
                return removed;

            foreach (var reaction in _tileReactions)
            {
                removed += reaction.TileReact(tile, this, reactVolume - removed);

                if (removed > reactVolume)
                    throw new Exception("Removed more than we have!");

                if (removed == reactVolume)
                    break;
            }

            return removed;
        }

        public void ReactionPlant(IEntity? plantHolder)
        {
            if (plantHolder == null || plantHolder.Deleted)
                return;

            foreach (var plantMetabolizable in _plantMetabolism)
            {
                plantMetabolizable.Metabolize(plantHolder, _customPlantMetabolism);
            }
        }
    }
}
