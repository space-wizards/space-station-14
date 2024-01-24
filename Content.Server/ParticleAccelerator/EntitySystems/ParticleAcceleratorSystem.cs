using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Projectiles;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Server.ParticleAccelerator.EntitySystems;

public sealed partial class ParticleAcceleratorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ProjectileSystem _projectileSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeControlBoxSystem();
        InitializePartSystem();
        InitializePowerBoxSystem();
    }
}
