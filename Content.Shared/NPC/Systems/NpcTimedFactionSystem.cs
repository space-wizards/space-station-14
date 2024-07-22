using Content.Shared.NPC.Components;
using Content.Shared.Timing;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.NPC.Prototypes;

namespace Content.Shared.NPC.Systems;

/// <summary>
/// Handles <see cref="NpcTimedFactionComponent"/> faction adding and removal.
/// </summary>
public sealed class NpcTimedFactionSystem : EntitySystem
{
    //[Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NpcTimedFactionComponent, MapInitEvent>(OnNpcTimedFactionMapInit);
        SubscribeLocalEvent<NpcTimedFactionComponent, NpcFactionSystem.TryRemoveFactionAttemptEvent>(OnTryRemoveFaction);

    }

    private void OnNpcTimedFactionMapInit(EntityUid uid, NpcTimedFactionComponent component, MapInitEvent args)
    {
        component.TimeFactionChange = _timing.CurTime + component.TimeUntilFactionChange;
        component.TimeFactionChangeBack = component.TimeFactionChange + component.TimeAsFaction;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var timedFactionQuery = EntityQueryEnumerator<NpcTimedFactionComponent>();
        while (timedFactionQuery.MoveNext(out var uid, out var component))
        {
            EnsureComp<NpcFactionMemberComponent>(uid, out var facComp);
            var faction = (uid, facComp);

            if (_timing.CurTime > component.TimeFactionChange)
            {
                _faction.AddFaction(faction, component.Faction);
                component.TimeFactionChange = component.TimeFactionChangeBack + component.TimeUntilFactionChange;
            }

            if (_timing.CurTime > component.TimeFactionChangeBack)
            {
                if (!_faction.IsStartingMember(faction, component.Faction))
                {
                    _faction.RemoveFaction(faction, component.Faction);
                }
                component.TimeFactionChangeBack = component.TimeFactionChange + component.TimeAsFaction;
            }
        }
    }

    private void OnTryRemoveFaction(Entity<NpcTimedFactionComponent> ent, ref NpcFactionSystem.TryRemoveFactionAttemptEvent args)
    {
        if (args.Faction == ent.Comp.Faction && ent.Comp.TimeFactionChangeBack <= ent.Comp.TimeFactionChange)
        {
            args.Cancel();
        }
    }
}
