#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class SpawnTile : IGraphAction
    {
        public string Tile { get; private set; } = string.Empty;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Tile, "tile", string.Empty);
        }

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted) return;

            var mapManager = IoCManager.Resolve<IMapManager>();
            var tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();

            var grid = mapManager.GetGrid(entity.Transform.GridID);
            grid.SetTile(entity.Transform.Coordinates, new Tile(tileDefinitionManager[Tile].TileId));
        }
    }
}
