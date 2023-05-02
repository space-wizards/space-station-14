using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.Revenant;
using Robust.Shared.Random;
using Robust.Shared.Map;
using Content.Shared.Tag;
using Content.Server.Storage.Components;
using Content.Server.Light.Components;
using Robust.Shared.Physics;
using Content.Shared.Throwing;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Shared.Item;
using Content.Shared.Bed.Sleep;
using System.Linq;
using Content.Server.Maps;
using Content.Server.Revenant.Components;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;
using Content.Shared.Changeling;

namespace Content.Server.EstacaoPirata.Changeling.EntitySystems;

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
