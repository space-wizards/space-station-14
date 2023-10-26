using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Console;
using Content.Shared.Administration;
using Content.Server.Administration;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.ErtCall
{

    public sealed class CallErtSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly MapLoaderSystem _map = default!;

        public bool SpawnErt(ErtCallPresetPrototype preset)
        {

            var shuttleMap = _mapManager.CreateMap();
            var options = new MapLoadOptions
            {
                LoadMap = true,
            };

            return (_map.TryLoad(shuttleMap, preset.Path, out _, options));
        }
    }
}

