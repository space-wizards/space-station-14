using Content.Shared.Damage.Components;
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

    public int CompareTo(IThresholdTrigger? other)
    {
        var comparison = 0;
        foreach (var trigger in Triggers)
        {
            comparison += trigger.CompareTo(other);
        }

        return Math.Clamp(comparison, -1, 1);
    }

    public bool Equals(IThresholdTrigger? other)
    {
        if (other is not AndTrigger trigger || trigger.Triggers.Count != Triggers.Count)
            return false;

        for (var i = 0; i < Triggers.Count; i++)
        {
            if (!trigger.Triggers[i].Equals(Triggers[i]))
                return false;
        }

        return true;
    }
}
