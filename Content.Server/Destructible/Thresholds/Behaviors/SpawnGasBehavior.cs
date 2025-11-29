using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SpawnGasBehavior : IThresholdBehavior
{
    [DataField("gasMixture", required: true)]
    public GasMixture Gas = new();

    public void Execute(EntityUid owner, DestructibleBehaviorSystem system, EntityUid? cause = null)
    {
        var atmosphereSystem = system.EntityManager.System<AtmosphereSystem>();

        var air = atmosphereSystem.GetContainingMixture(owner, false, true);

        if (air != null)
            atmosphereSystem.Merge(air, Gas);
    }
}
