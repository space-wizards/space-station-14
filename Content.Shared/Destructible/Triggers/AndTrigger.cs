using Content.Shared.Damage;
using Robust.Shared.Serialization;

namespace Content.Shared.Destructible.Thresholds.Triggers;

/// <summary>
/// A trigger that will activate when all of its triggers have activated.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class AndTrigger : IThresholdTrigger
{
    [DataField]
    public List<IThresholdTrigger> Triggers = new();

    public bool Reached(Entity<DamageableComponent> damageable, SharedDestructibleSystem system)
    {
        foreach (var trigger in Triggers)
        {
            if (!trigger.Reached(damageable, system))
            {
                return false;
            }
        }

        return true;
    }
}
