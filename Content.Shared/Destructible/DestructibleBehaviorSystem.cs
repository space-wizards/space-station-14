using JetBrains.Annotations;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Body.Systems;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Fluids;
using Content.Shared.Trigger.Systems;
using Content.Shared.Administration.Logs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Destructible;

// TODO: Remove all this crap after the system transition to entity effects.
[UsedImplicitly]
public sealed partial class DestructibleBehaviorSystem : EntitySystem
{
    [Dependency] public readonly IRobustRandom Random = default!;
    public new IEntityManager EntityManager => base.EntityManager;

    [Dependency] public readonly SharedAtmosphereSystem AtmosphereSystem = default!;
    [Dependency] public readonly SharedAudioSystem AudioSystem = default!;
    [Dependency] public readonly SharedBodySystem BodySystem = default!;
    [Dependency] public readonly SharedExplosionSystem ExplosionSystem = default!;
    [Dependency] public readonly SharedStackSystem StackSystem = default!;
    [Dependency] public readonly TriggerSystem TriggerSystem = default!;
    [Dependency] public readonly SharedDestructibleSystem DestructibleSystem = default!;
    [Dependency] public readonly SharedSolutionContainerSystem SolutionContainerSystem = default!;
    [Dependency] public readonly SharedPuddleSystem PuddleSystem = default!;
    [Dependency] public readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] public readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] public readonly ISharedAdminLogManager AdminLogger = default!;
}

// Currently only used for destructible integration tests. Unless other uses are found for this, maybe this should just be removed and the tests redone.
/// <summary>
///     Event raised when a <see cref="DamageThreshold"/> is reached.
/// </summary>
public sealed class DamageThresholdReached : EntityEventArgs
{
    public readonly DestructibleComponent Parent;

    public readonly DamageThreshold Threshold;

    public DamageThresholdReached(DestructibleComponent parent, DamageThreshold threshold)
    {
        Parent = parent;
        Threshold = threshold;
    }
}
