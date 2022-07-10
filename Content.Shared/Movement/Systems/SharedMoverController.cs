using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Tag;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController : VirtualController
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("mover");
        InitializeInput();
        InitializeMobMovement();
    }

    public override void Shutdown()
    {
        ShutdownInput();
        ShutdownMobMovement();
    }

    public void ChangeTo(EntityUid uid, MoverComponent component)
    {

    }
}
