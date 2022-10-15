using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Components;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    /// <summary>
    ///     Returns a list of ValueTuples of <see cref="T"/> and OrganComponent on each organ
    ///     in the given body.
    /// </summary>
    /// <param name="uid">The entity to check for the component on.</param>
    /// <param name="body">The body to check for organs on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    public List<(T Comp, BodyComponent Organ)> GetOrganComponents<T>(
        EntityUid uid,
        BodyComponent? body = null)
        where T : Component
    {
        if (!Resolve(uid, ref body))
            return new List<(T Comp, BodyComponent Organ)>();

        var query = EntityManager.GetEntityQuery<T>();
        var list = new List<(T Comp, BodyComponent Organ)>(3);
        foreach (var organ in GetChildOrgans(uid, body))
        {
            if (query.TryGetComponent(organ.Owner, out var comp))
                list.Add((comp, organ));
        }

        return list;
    }

    /// <summary>
    ///     Tries to get a list of ValueTuples of <see cref="T"/> and OrganComponent on each organs
    ///     in the given body.
    /// </summary>
    /// <param name="uid">The entity to check for the component on.</param>
    /// <param name="comps">The list of components.</param>
    /// <param name="body">The body to check for organs on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    /// <returns>Whether any were found.</returns>
    public bool TryGetOrganComponents<T>(
        EntityUid uid,
        [NotNullWhen(true)] out List<(T Comp, BodyComponent Organ)>? comps,
        BodyComponent? body = null)
        where T : Component
    {
        if (!Resolve(uid, ref body))
        {
            comps = null;
            return false;
        }

        comps = GetOrganComponents<T>(uid, body);

        if (comps.Count == 0)
        {
            comps = null;
            return false;
        }

        return true;
    }
}
