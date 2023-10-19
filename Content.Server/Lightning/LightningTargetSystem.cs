using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning.Components;
using Content.Server.Lightning.Events;
using Content.Server.Power.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// The system is responsible for switching the status of the active lightning target
/// </summary>
public sealed class LightningTargetSystem : EntitySystem
{

    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

}
