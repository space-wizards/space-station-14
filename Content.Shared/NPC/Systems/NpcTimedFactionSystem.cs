using Content.Shared.NPC.Components;
using Content.Shared.Timing;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.NPC.Prototypes;


namespace Content.Shared.NPC.Systems;

/// <summary>
/// Handles <see cref="NpcTimedFactionComponent"/> faction adding and removal.
/// </summary>
public sealed partial class NpcTimedFactionSystem : EntitySystem
{
    //[Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NpcTimedFactionComponent, MapInitEvent>(OnNpcTimedFactionMapInit);
    //    SubscribeLocalEvent<NpcTimedFactionComponent, FactionChangeEvent>(OnFactionChange);


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
            if (!TryComp<NpcFactionMemberComponent>(uid, out var facComp) || facComp == null)
                continue;
            var faction = (uid, facComp);

            if (_timing.CurTime >= component.TimeFactionChange)
            {
                // TODO: change the faction to faction A via event
                _faction.AddFaction(faction, component.Faction);
                component.TimeFactionChange = component.TimeFactionChangeBack + component.TimeUntilFactionChange;
            }

            if (_timing.CurTime >= component.TimeFactionChangeBack)
            {
                // TODO: change the faction back via event
                _faction.RemoveFaction(faction, component.Faction);
                component.TimeFactionChangeBack = component.TimeFactionChange + component.TimeAsFaction;
            }
        }
    }
}
