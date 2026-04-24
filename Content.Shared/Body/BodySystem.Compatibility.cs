using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

public sealed partial class BodySystem
{
    /// <summary>
    /// Returns a list of organs with a given component in the body.
    /// This is only provided to ease migration from the older BodySystem and should not be used in new code.
    /// </summary>
    /// <param name="ent">The body to query.</param>
    /// <param name="organs">The set of organs with the given component.</param>
    /// <typeparam name="TComp">The component to test for.</typeparam>
    /// <returns>Whether any organs were returned.</returns>
    [Obsolete("Use an event-relay based approach instead")]
    [PublicAPI]
    public bool TryGetOrgansWithComponent<TComp>(Entity<BodyComponent?> ent, out List<Entity<TComp>> organs) where TComp : Component
    {
        organs = new();
        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return false;

        foreach (var organ in ent.Comp.Organs?.ContainedEntities ?? [])
        {
            if (TryComp<TComp>(organ, out var comp))
                organs.Add((organ, comp));
        }

        return organs.Count != 0;
    }

    // Begin Offbrand Additions
    /// <summary>
    /// Returns a list of organs with a given category in the body.
    /// TODO: new surgery
    /// </summary>
    /// <param name="ent">The body to query.</param>
    /// <param name="organs">The set of organs with the given category.</param>
    /// <param name="category">The category to look up.</param>
    /// <typeparam name="TComp">The component to test for.</typeparam>
    /// <returns>Whether any organs were returned.</returns>
    [Obsolete("Use an event-relay based approach instead")]
    [PublicAPI]
    public bool TryGetOrgansWithCategoryAndComponent<TComp>(Entity<BodyComponent?> ent, out List<Entity<OrganComponent, TComp>> organs, ProtoId<OrganCategoryPrototype> category) where TComp : Component
    {
        organs = new();
        if (!_bodyQuery.Resolve(ent, ref ent.Comp))
            return false;

        foreach (var organ in ent.Comp.Organs?.ContainedEntities ?? [])
        {
            if (TryComp<OrganComponent>(organ, out var comp) && comp.Category == category && TryComp<TComp>(organ, out var tcomp))
                organs.Add((organ, comp, tcomp));
        }

        return organs.Count != 0;
    }
    // End Offbrand Additions
}
