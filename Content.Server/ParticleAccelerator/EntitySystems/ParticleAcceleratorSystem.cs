using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Projectiles;
using Content.Server.Machines.EntitySystems;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Server.ParticleAccelerator.EntitySystems;

public sealed partial class ParticleAcceleratorSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private ProjectileSystem _projectileSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private MultipartMachineSystem _multipartMachine = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeControlBoxSystem();
        InitializePowerBoxSystem();
    }
}
