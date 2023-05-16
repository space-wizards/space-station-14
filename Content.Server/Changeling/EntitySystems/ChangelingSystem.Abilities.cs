using Robust.Shared.Map;
using Content.Server.Maps;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    private void InitializeAbilities()
    {

    }


}
