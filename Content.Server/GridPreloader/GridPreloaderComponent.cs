using Content.Shared.GridPreloader.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.GridPreloader;

/// <summary>
/// component storing data about preloaded grids and their location
/// </summary>
[RegisterComponent, Access(typeof(GridPreloaderSystem))]
public sealed partial class GridPreloaderComponent : Component
{
    [DataField]
    public MapId? PreloadGridsMapId;

    [DataField]
    public List<(ProtoId<PreloadedGridPrototype>, EntityUid?)> PreloadedGrids = new();
}
