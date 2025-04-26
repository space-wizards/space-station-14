using JetBrains.Annotations;

namespace Content.Shared._Harmony.EntitySelector;

/// <summary>
/// Provides an API for using an <see cref="EntitySelector"/>
/// </summary>
public sealed class EntitySelectorManager
{
    [PublicAPI]
    public static bool EntityMatchesAny(EntityUid entity, IEnumerable<EntitySelector> selectors)
    {
        foreach (var selector in selectors)
        {
            if (selector.Matches(entity))
                return true;
        }

        return false;
    }

    [PublicAPI]
    public static IEnumerable<EntityUid> AllMatchingEntities(IEnumerable<EntityUid> entities, EntitySelector selector)
    {
        foreach (var entity in entities)
        {
            if (selector.Matches(entity))
                yield return entity;
        }
    }

    [PublicAPI]
    public static IEnumerable<EntityUid> AllEntitiesMatchingAny(
        IEnumerable<EntityUid> entities,
        List<EntitySelector> selectors)
    {
        foreach (var entity in entities)
        {
            foreach (var selector in selectors)
            {
                if (!selector.Matches(entity))
                    continue;

                yield return entity;
                break;
            }
        }
    }
}
