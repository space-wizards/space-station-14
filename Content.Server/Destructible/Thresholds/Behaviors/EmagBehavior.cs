using Content.Shared.Emag.Systems;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[Serializable, DataDefinition]
public sealed partial class EmagBehavior : IThresholdBehavior
{
    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        var emag = system.EntityManager.System<EmagSystem>();
        // fallback to saying the entity emagged itself if there was no user
        emag.DoEmagEffect(cause ?? owner, owner);
    }
}
