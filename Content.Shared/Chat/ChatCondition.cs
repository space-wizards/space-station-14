using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Chat;

[Serializable]
[DataDefinition]
[Virtual]
public partial class ChatCondition
{
    /// <summary>
    /// When set, indicates what type of consumer hashset is being used for this chat condition, so that the consumers can be converted if necessary.
    /// Should only support ICommonSession, EntityUid or null.
    /// If set to null, the chat condition must not utilize the consumer hashset (useful for binary all-or-none conditions).
    /// </summary>
    public virtual Type? ConsumerType { get; set; } = typeof(ICommonSession);

    // If true, invert the result of the condition.
    [DataField]
    public bool Inverted = false;

    /// <summary>
    /// If true, the sessions' entities that pass this condition will also be evaluated against the consumerCollection's
    /// ConsumeEntityChatConditions.
    /// Only applicable when evaluating consumers, and not on any subcondition SessionChatConditions.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public bool UseConsumeEntityChatConditions = false;

    /// <summary>
    /// Defines conditions that act as logical operators on this condition's hashset of consumers.
    /// Any subcondition acts as "AND" operators on -this- condition's consumers.
    /// Relative to each other they act as "OR" operators (as they each only act on this condition's consumers).
    /// To have multiple "AND" operations, the conditions must be daisy-chained on each condition's Subcondition list:
    /// </summary>
    [DataField]
    public List<ChatCondition> Subconditions = new List<ChatCondition>();

    /// <summary>
    /// Apply the specific filter of this ChatCondition.
    /// When constructing a filter the use of the "consumers" parameter is optional, as any subfilter will apply AND/OR operators regardless.
    /// However it might be used to save on processing load when evaluating certain filters, e.g. when checking for components common on many entities.
    /// </summary>
    /// <param name="consumers">Consumers to run this ChatCondition on.</param>
    /// <returns></returns>
    public virtual HashSet<T> FilterConsumers<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters) { return consumers; }

    public ChatCondition() { }

    public ChatCondition(List<ChatCondition> subconditions)
    {
        Subconditions = subconditions;
    }

    /// <summary>
    /// Processes this condition and all subconditions.
    /// </summary>
    /// <param name="consumers">The hashset of consumers that should be evaluated against.</param>
    /// <returns>Hashset of consumers processed in this conditions and its subconditions.</returns>
    public HashSet<T> ProcessCondition<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    {
        HashSet<T> filtered;

        if (!Inverted)
            filtered = FilterConsumers(consumers, channelParameters);
        else
            filtered = consumers.Except(FilterConsumers(consumers, channelParameters)).ToHashSet();

        if (Subconditions.Count > 0)
        {
            filtered = IterateSubconditions(filtered, channelParameters);
        }

        //var entities = filtered.Where(z => z.AttachedEntity != null).Select(z => z.AttachedEntity!.Value).ToHashSet();

        return filtered;
    }

    /// <summary>
    /// Iterate over all the subconditions and process them.
    /// </summary>
    /// <param name="consumers">The hashset of consumers that should be evaluated against.</param>
    /// <returns>Hashset of consumers processed by the subconditions.</returns>
    private HashSet<T> IterateSubconditions<T>(HashSet<T> consumers, Dictionary<Enum, object> channelParameters)
    {
        var changedConsumers = new HashSet<T>();
        foreach (var condition in Subconditions)
        {
            // No more consumers, no point in continuing further.
            if (changedConsumers.Count == consumers.Count)
                return changedConsumers;

            // If the condition doesn't do anything specific with the type, or if it's the same type as the input, just run it normally
            if (condition.ConsumerType == null || typeof(T) == condition.ConsumerType)
            {
                changedConsumers.UnionWith(condition.ProcessCondition(consumers, channelParameters));
            }
            // Converts the hashset of ICommonSessions to EntityUid, processes the condition, and then converts the result back to ICommonSessions
            else if (condition.ConsumerType == typeof(EntityUid) && consumers is HashSet<ICommonSession> sessionConsumers)
            {
                var sessionEntities = sessionConsumers.Where(z => z.AttachedEntity != null).Select(z => z.AttachedEntity!.Value).ToHashSet();
                var filteredEntities = condition.ProcessCondition<EntityUid>(sessionEntities, channelParameters);
                var filteredSessions = sessionConsumers.Where(z => filteredEntities.Contains(z.AttachedEntity ?? EntityUid.Invalid)).ToHashSet();
                changedConsumers.UnionWith(filteredSessions as HashSet<T> ?? new HashSet<T>());
            }
            // Converts the hashset of EntityUid to ICommonSessions, processes the condition, and then converts the result back to EntityUid
            else if (condition.ConsumerType == typeof(ICommonSession) && consumers is HashSet<EntityUid> entityConsumers)
            {
                var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
                if (entitySystemManager.TryGetEntitySystem<ActorSystem>(out var actorSystem))
                {

                    HashSet<ICommonSession> entitySessions = new();
                    foreach (var entity in entityConsumers)
                    {
                        if (actorSystem.TryGetSession(entity, out var session) && session != null)
                            entitySessions.Add(session);
                    }

                    var filteredSessions =
                        condition.ProcessCondition<ICommonSession>(entitySessions, channelParameters);
                    var filteredEntities = filteredSessions.Where(z => z.AttachedEntity != null)
                        .Select(z => z.AttachedEntity!.Value)
                        .ToHashSet();

                    changedConsumers.UnionWith(filteredEntities as HashSet<T> ?? new HashSet<T>());
                }
            }
        }
        return changedConsumers;
    }
}

