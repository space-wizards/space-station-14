using JetBrains.Annotations;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SpawnGasBehavior : IThresholdBehavior
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    [DataField("gasMixture", required: true)]
    public GasMixture Gas = new();

    public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        var air = _atmosphere.GetContainingMixture(owner, false, true);
        if (air != null)
            _atmosphere.Merge(air, Gas);
    }
}
