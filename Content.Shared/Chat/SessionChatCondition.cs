using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Chat;

[Serializable]
[DataDefinition]
[Virtual]
public partial class SessionChatCondition
{
    // If true, invert the result of the condition.
    [DataField]
    public bool Inverted = false;

    /// <summary>
    /// Defines conditions that act as logical operators on this condition's hashset of consumers.
    /// Any subcondition acts as "AND" operators on -this- condition's consumers.
    /// Relative to each other they act as "OR" operators (as they each only act on this condition's consumers).
    /// To have multiple "AND" operations, the conditions must be daisy-chained on each condition's Subcondition list:
    /// </summary>
    [DataField]
    public List<SessionChatCondition> Subconditions = new List<SessionChatCondition>();

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
    public virtual HashSet<ICommonSession> FilterConsumers(HashSet<ICommonSession> consumers, Dictionary<Enum, object> channelParameters) { return consumers; }

    public SessionChatCondition() { }

    public SessionChatCondition(List<SessionChatCondition> subconditions, List<EntityChatCondition> entityChatConditions)
    {
        Subconditions = subconditions;
        EntityChatConditions = entityChatConditions;
    }

    /// <summary>
    /// Processes this condition and all subconditions.
    /// </summary>
    /// <param name="consumers">The hashset of consumers that should be evaluated against.</param>
    /// <returns>Hashset of consumers processed in this conditions and its subconditions.</returns>
    public HashSet<ICommonSession> ProcessCondition(HashSet<ICommonSession> consumers, Dictionary<Enum, object> channelParameters)
    {
        var filtered = new HashSet<ICommonSession>();
        if (!Inverted)
            filtered = FilterConsumers(consumers, channelParameters);
        else
            filtered = consumers.Except(FilterConsumers(consumers, channelParameters)).ToHashSet();

        Logger.Debug("eh? " + filtered.Count.ToString() + " subconditions: " + Subconditions.Count.ToString());
        if (Subconditions.Count > 0)
        {
            filtered = IterateSessionSubconditions(filtered, channelParameters);
            Logger.Debug("eh2? " + filtered.Count.ToString());
        }

        Logger.Debug("eh3? " + EntityChatConditions.Count.ToString());

        if (EntityChatConditions.Count > 0)
        {
            var entities = filtered.Where(z => z.AttachedEntity != null).Select(z => z.AttachedEntity!.Value).ToHashSet();

            if (entities.Count > 0)
                entities = IterateEntitySubconditions(entities, channelParameters);

            filtered = filtered.Where(z => entities.Contains(z.AttachedEntity ?? EntityUid.Invalid)).ToHashSet();
        }

        return filtered;
    }

    /// <summary>
    /// Iterate over all the subconditions and process them.
    /// </summary>
    /// <param name="consumers">The hashset of consumers that should be evaluated against.</param>
    /// <returns>Hashset of consumers processed by the subconditions.</returns>
    private HashSet<EntityUid> IterateEntitySubconditions(HashSet<EntityUid> consumers, Dictionary<Enum, object> channelParameters)
    {
        var changedConsumers = new HashSet<EntityUid>();
        foreach (var condition in EntityChatConditions)
        {
            // No more consumers, no point in continuing further.
            if (changedConsumers.Count == consumers.Count)
                return changedConsumers;

            if (!condition.Inverted)
                changedConsumers.UnionWith(condition.ProcessCondition(consumers, channelParameters));
            else
                changedConsumers.UnionWith(consumers.Except(condition.ProcessCondition(consumers, channelParameters)));
        }

        return changedConsumers;
    }

    /// <summary>
    /// Iterate over all the subconditions and process them.
    /// </summary>
    /// <param name="consumers">The hashset of consumers that should be evaluated against.</param>
    /// <returns>Hashset of consumers processed by the subconditions.</returns>
    private HashSet<ICommonSession> IterateSessionSubconditions(HashSet<ICommonSession> consumers, Dictionary<Enum, object> channelParameters)
    {
        Logger.Debug("getting tired: " + Subconditions.Count() + " " + consumers.Count());
        var changedConsumers = new HashSet<ICommonSession>();
        foreach (var condition in Subconditions)
        {
            // No more consumers, no point in continuing further.
            if (changedConsumers.Count == consumers.Count)
                return changedConsumers;

            Logger.Debug("wehehe " + changedConsumers.Count.ToString());
            Logger.Debug("wehehetype " + condition.GetType().Name);

            changedConsumers.UnionWith(condition.ProcessCondition(consumers, channelParameters));

            Logger.Debug("wehehe2 " + changedConsumers.Count.ToString());
        }
        return changedConsumers;
    }
}

