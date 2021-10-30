using Content.Shared.Maps;
using Content.Shared.Window;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class EmptyOrWindowValidInTile : IConstructionCondition
    {
        [DataField("tileNotBlocked")]
        private readonly TileNotBlocked _tileNotBlocked = new();

        public bool Condition(IEntity user, EntityCoordinates location, Direction direction)
        {
            var result = false;

            foreach (var entity in location.GetEntitiesInTile(LookupFlags.Approximate | LookupFlags.IncludeAnchored))
            {
                if (entity.HasComponent<SharedCanBuildWindowOnTopComponent>())
                    result = true;
            }

            if (!result)
                result = _tileNotBlocked.Condition(user, location, direction);

            return result;
        }
    }
}
