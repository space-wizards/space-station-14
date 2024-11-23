using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Chat;

[Serializable]
[DataDefinition]
[Virtual]
public partial class SessionChatCondition
{

    /// <summary>
    /// Defines conditions that act as logical operators on this condition's hashset of consumers.
    /// Any subcondition acts as "AND" operators on -this- condition's consumers.
    /// Relative to each other they act as "OR" operators (as they each only act on this condition's consumers).
    /// To have multiple "AND" operations, the conditions must be daisy-chained on each condition's Subcondition list:
    /// </summary>
    [DataField]
    public List<EntityChatCondition> EntityChatConditions = new List<EntityChatCondition>();

    /// <summary>
    /// Apply the specific filter of this ChatCondition.
    /// When constructing a filter the use of the "consumers" parameter is optional, as any subfilter will apply AND/OR operators regardless.
    /// However it might be used to save on processing load when evaluating certain filters, e.g. when checking for components common on many entities.
    /// </summary>
    /// <param name="consumers">Consumers to run this ChatCondition on.</param>
    /// <returns></returns>
    public virtual HashSet<ICommonSession> FilterConsumers(HashSet<ICommonSession> consumers) { return consumers; }

    public SessionChatCondition() { }

    public SessionChatCondition(List<EntityChatCondition> entityChatConditions)
    {
        EntityChatConditions = entityChatConditions;
    }

    /// <summary>
    /// Processes this condition and all subconditions.
    /// </summary>
    /// <param name="consumers">The hashset of consumers that should be evaluated against.</param>
    /// <returns>Hashset of consumers processed in this conditions and its subconditions.</returns>
    public HashSet<ICommonSession> ProcessCondition(HashSet<ICommonSession> consumers, EntityUid? senderEntity)
    {
        var filtered = FilterConsumers(consumers);

        if (EntityChatConditions.Count > 0)
        {
            var entities = filtered.Where(z => z.AttachedEntity != null).Select(z => z.AttachedEntity!.Value).ToHashSet();

            if (entities.Count > 0)
                entities = IterateSubconditions(entities, senderEntity);

            filtered = filtered.Where(z => entities.Contains(z.AttachedEntity ?? EntityUid.Invalid)).ToHashSet();
        }

        return filtered;
    }

    /// <summary>
    /// Iterate over all the subconditions and process them.
    /// </summary>
    /// <param name="consumers">The hashset of consumers that should be evaluated against.</param>
    /// <returns>Hashset of consumers processed by the subconditions.</returns>
    private HashSet<EntityUid> IterateSubconditions(HashSet<EntityUid> consumers, EntityUid? senderEntity)
    {
        var changedConsumers = consumers;
        foreach (var condition in EntityChatConditions)
        {
            // No more consumers, no point in continuing further.
            if (changedConsumers.Count == 0)
                return changedConsumers;

            if (!condition.Inverted)
                changedConsumers.IntersectWith(condition.ProcessCondition(consumers, senderEntity));
            else
                changedConsumers.ExceptWith(condition.ProcessCondition(consumers, senderEntity));
        }

        return changedConsumers;
    }
}

