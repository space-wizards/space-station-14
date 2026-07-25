// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Mind;
using Content.Shared.Mind.Filters;
using Robust.Shared.Map;

namespace Content.Shared.DeadSpace.Mind.Filters;

/// <summary>
/// Keeps only minds whose current bodies are on the same map as the excluded mind's current body.
/// </summary>
public sealed partial class SameMapMindFilter : MindFilter
{
    protected override bool ShouldRemove(
        Entity<MindComponent> mind,
        EntityUid? exclude,
        IEntityManager entMan,
        SharedMindSystem mindSys)
    {
        if (mind.Comp.OwnedEntity is not {} body ||
            exclude is not {} excludeMind ||
            !entMan.TryGetComponent(excludeMind, out MindComponent? excludeMindComp) ||
            excludeMindComp.OwnedEntity is not {} excludeBody ||
            !entMan.TryGetComponent(body, out TransformComponent? bodyTransform) ||
            !entMan.TryGetComponent(excludeBody, out TransformComponent? excludeTransform))
        {
            return true;
        }

        return excludeTransform.MapID == MapId.Nullspace ||
               bodyTransform.MapID != excludeTransform.MapID;
    }
}
