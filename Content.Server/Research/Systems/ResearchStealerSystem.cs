using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;

namespace Content.Server.Research.Systems;

public sealed class ResearchStealerSystem : SharedResearchStealerSystem
{
    [Dependency] private readonly SharedResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchStealerComponent, ResearchStealDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(EntityUid uid, ResearchStealerComponent comp, ResearchStealDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!TryComp<TechnologyDatabaseComponent>(target, out var database))
            return;

        var ev = new ResearchStolenEvent(uid, target, database.UnlockedTechnologies);
        RaiseLocalEvent(uid, ref ev);
        // oops, no more advanced lasers!
        _research.ClearTechs(target, database);
    }
}

/// <summary>
/// Event raised on the user when research is stolen from a R&D server.
/// Techs contains every technology id researched.
/// </summary>
[ByRefEvent]
public record struct ResearchStolenEvent(EntityUid Used, EntityUid Target, List<String> Techs);
