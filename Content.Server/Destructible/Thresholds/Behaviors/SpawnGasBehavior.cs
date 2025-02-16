using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Destructible.Thresholds.Behaviors;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SpawnGasBehavior : IThresholdBehavior
{
    [DataField("gasMixture", required: true)]
    public GasMixture Gas = new();

    public void Execute(EntityUid owner,
        IDependencyCollection collection,
        EntityManager entManager,
        EntityUid? cause = null)
    {
        var atmosSystem = entManager.System<AtmosphereSystem>();
        var air = atmosSystem.GetContainingMixture(owner, false, true);

        if (air != null)
            atmosSystem.Merge(air, Gas);
    }
}
