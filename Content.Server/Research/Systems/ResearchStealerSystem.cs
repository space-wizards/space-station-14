using Content.Shared.Research.Components;

namespace Content.Server.Research.Systems;

public sealed class ResearchStealerSystem : SharedResearchStealerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchStealerComponent, DownloadDoAfterEvent>(OnDownloadDoAfter);
    }

    private void OnDoAfter(EntityUid uid, ResearchStealerComponent comp, ResearchStealDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var target = args.Target;

        if (!TryComp<TechnologyDatabaseComponent>(target, out var database))
            return;
        var ev = new ResearchStolenEvent(uid, target, database.UnlockedTechnologies);
        RaiseNewLocalEvent(args.User, ref ev);
        // oops, no more advanced lasers!
        database.UnlockedTechnologies.Clear();
    }
}

/// <summary>
/// Event raised on the user when research is stolen from a R&D server.
/// Techs contains every technology id researched.
/// </summary>
[ByRefEvent]
public record struct ResearchStolenEvent(EntityUid Used, EntityUid Target, HashSet<String> Techs);
