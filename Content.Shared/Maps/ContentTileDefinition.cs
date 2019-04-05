using JetBrains.Annotations;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

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
        public bool CanCrowbar { get; private set; }
        public string FootstepSounds { get; private set; }

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

            if (mapping.TryGetNode("can_crowbar", out node))
            {
                CanCrowbar = node.AsBool();
            }

            if (mapping.TryGetNode("footstep_sounds", out node))
            {
                FootstepSounds = node.AsString();
            }
        }
    }
}
