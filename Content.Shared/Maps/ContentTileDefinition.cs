using JetBrains.Annotations;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Maps
{
    [UsedImplicitly]
    [Prototype("tile")]
    public sealed class ContentTileDefinition : IPrototype, IIndexedPrototype, ITileDefinition
    {
        string IIndexedPrototype.ID => Name;

        public string Name { get; private set; }
        public ushort TileId { get; private set; }
        public string DisplayName { get; private set; }
        public string SpriteName { get; private set; }
        public bool IsSubFloor { get; private set; }
        public string SubFloor { get; private set; }
        public bool CanCrowbar { get; private set; }
        public string FootstepSounds { get; private set; }
        public float Friction { get; set; }
        public string ItemDropPrototypeName { get; private set; }

        public void AssignTileId(ushort id)
        {
            TileId = id;
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            Name = mapping.GetNode("name").ToString();
            DisplayName = mapping.GetNode("display_name").ToString();
            SpriteName = mapping.GetNode("texture").ToString();

            if (mapping.TryGetNode("is_subfloor", out var node))
            {
                IsSubFloor = node.AsBool();
            }
            if (mapping.TryGetNode("subfloor", out var another_node))
            {
                SubFloor = another_node.AsString();
            }

            if (mapping.TryGetNode("can_crowbar", out node))
            {
                CanCrowbar = node.AsBool();
            }

            if (mapping.TryGetNode("footstep_sounds", out node))
            {
                FootstepSounds = node.AsString();
            }

            if (mapping.TryGetNode("friction", out node))
            {
                Friction = node.AsFloat();
            }
            else
            {
                Friction = 0;
            }

            if (mapping.TryGetNode("item_drop", out node))
            {
                ItemDropPrototypeName = node.ToString();
            }
            else
            {
                ItemDropPrototypeName = "FloorTileItemSteel";
            }
        }

    }
}
