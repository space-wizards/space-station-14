using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class TileType : IConstructionCondition
    {
        [DataField("targets")]
        public List<string> TargetTiles { get; private set; } = new();

        [DataField("guideText")]
        public string? GuideText;

        [DataField("guideIcon")]
        public SpriteSpecifier? GuideIcon;

        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            var tileFound = location.GetTileRef();

            if (tileFound == null)
                return false;

            var tile = tileFound.Value.Tile.GetContentTileDefinition();
            foreach (var targetTile in TargetTiles)
            {
                if (tile.ID == targetTile)
                    return true;
            }
            return false;
        }

        public ConstructionGuideEntry? GenerateGuideEntry()
        {
            if (GuideText == null)
                return null;

            return new ConstructionGuideEntry()
            {
                Localization = GuideText,
                Icon = GuideIcon,
            };
        }
    }
}
