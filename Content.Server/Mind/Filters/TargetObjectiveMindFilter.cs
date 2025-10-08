using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Filters;
using Content.Shared.Whitelist;

namespace Content.Server.Mind.Filters;

/// <summary>
/// A mind filter that removes minds if you have an objective targeting them matching a blacklist.
/// </summary>
/// <remarks>
/// Used to prevent assigning multiple kill objectives for the same person.
/// </remarks>
public sealed partial class TargetObjectiveMindFilter : MindFilter
{
    /// <summary>
    /// A blacklist to check objectives against, for removing a mind.
    /// If null then any objective targeting it will remove minds.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? excluded, IEntityManager entMan, SharedMindSystem mindSys)
    {
        // ignore this filter if there is no user to check
        if (!entMan.TryGetComponent<MindComponent>(excluded, out var excludedMind))
            return false;

        var whitelistSys = entMan.System<EntityWhitelistSystem>();
        foreach (var objective in excludedMind.Objectives)
        {
            // if the player has an objective targeting this mind
            if (entMan.TryGetComponent<TargetObjectiveComponent>(objective, out var kill) && kill.Target == mind.Owner)
            {
                // remove the mind if this objective is blacklisted
                if (whitelistSys.IsBlacklistPassOrNull(Blacklist, objective))
                    return true;
            }
        }

        return false;
    }
}
