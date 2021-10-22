using System.Threading.Tasks;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Maps;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tools.Components
{
    [RegisterComponent]
    public class TilePryingComponent : Component, IAfterInteract
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override string Name => "TilePrying";

        [DataField("toolComponentNeeded")]
        private bool _toolComponentNeeded = true;

        [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        private string _qualityNeeded = "Prying";

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            TryPryTile(eventArgs.User, eventArgs.ClickLocation);
            return true;
        }

        public async void TryPryTile(IEntity user, EntityCoordinates clickLocation)
        {
            if (!Owner.TryGetComponent<ToolComponent>(out var tool) && _toolComponentNeeded)
                return;

            if (!_mapManager.TryGetGrid(clickLocation.GetGridId(Owner.EntityManager), out var mapGrid))
                return;

            var tile = mapGrid.GetTileRef(clickLocation);

            var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

            if (!user.InRangeUnobstructed(coordinates, popup: false))
                return;

            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

            if (!tileDef.CanCrowbar)
                return;

            if (_toolComponentNeeded && !await EntitySystem.Get<ToolSystem>().UseTool(Owner.Uid, user.Uid, null, 0f, 0f, _qualityNeeded, toolComponent:tool))
                return;

            coordinates.PryTile(Owner.EntityManager, _mapManager);
        }
    }
}
