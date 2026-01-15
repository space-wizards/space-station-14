using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[Serializable]
[DataDefinition]
public sealed partial class DumpCanisterBehavior : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly GasCanisterSystem _gasCanister = default!;

    public void Execute(EntityUid owner, EntityUid? cause = null)
    {
        _gasCanister.PurgeContents(owner);
    }
}
