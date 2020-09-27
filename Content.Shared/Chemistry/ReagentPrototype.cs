using System;
using System.Collections.Generic;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.Chemistry;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Chemistry
{
    [Prototype("reagent")]
    public class ReagentPrototype : IPrototype, IIndexedPrototype
    {
        [Dependency] private readonly IModuleManager _moduleManager = default!;

        private string _id;
        private string _name;
        private string _description;
        private string _physicalDescription;
        private Color _substanceColor;
        private List<IMetabolizable> _metabolism;
        private string _spritePath;
        private List<ITileReaction> _tileReactions;

        public string ID => _id;
        public string Name => _name;
        public string Description => _description;
        public string PhysicalDescription => _physicalDescription;
        public Color SubstanceColor => _substanceColor;

        //List of metabolism effects this reagent has, should really only be used server-side.
        public List<IMetabolizable> Metabolism => _metabolism;
        public List<ITileReaction> TileReactions => _tileReactions;
        public string SpriteReplacementPath => _spritePath;

        public ReagentPrototype()
        {
            IoCManager.InjectDependencies(this);
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _description, "desc", string.Empty);
            serializer.DataField(ref _physicalDescription, "physicalDesc", string.Empty);
            serializer.DataField(ref _substanceColor, "color", Color.White);
            serializer.DataField(ref _spritePath, "spritePath", string.Empty);

            if (_moduleManager.IsServerModule)
            {
                serializer.DataField(ref _metabolism, "metabolism", new List<IMetabolizable> { new DefaultMetabolizable() });
                serializer.DataField(ref _tileReactions, "tileReactions", new List<ITileReaction> { });
            }
            else
            {
                _metabolism = new List<IMetabolizable> { new DefaultMetabolizable() };
                _tileReactions = new List<ITileReaction>();
            }
        }

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

        public ReagentUnit ReactionEntity(IEntity entity, ReactionMethod method, ReagentUnit reactVolume)
        {
            var removed = ReagentUnit.Zero;

            foreach (var react in entity.GetAllComponents<IReagentReaction>())
            {
                switch (method)
                {
                    case ReactionMethod.Touch:
                        removed += react.ReagentReactTouch(this, reactVolume);
                        break;
                    case ReactionMethod.Ingestion:
                        removed += react.ReagentReactIngestion(this, reactVolume);
                        break;
                    case ReactionMethod.Injection:
                        removed += react.ReagentReactInjection(this, reactVolume);
                        break;
                }

                if (removed > reactVolume)
                    throw new Exception("Removed more than we have!");

                if (removed == reactVolume)
                    break;
            }

            return removed;
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
    }
}
