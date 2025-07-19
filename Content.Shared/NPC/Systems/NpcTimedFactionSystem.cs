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
    [Dependency] private readonly IRobustRandom _random = default!;
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
                component.TimeFactionChange = component.TimeFactionChangeBack + component.TimeUntilFactionChange + TimeSpan.FromSeconds(_random.Next(component.RandomBonusTimeUntilFactionChange + 1));

                if (!component.HasChangedOnce)
                {
                    component.HasChangedOnce = true; // Used to prevent event cancellations before the component has triggered.
                }
            }

            if (_timing.CurTime > component.TimeFactionChangeBack)
            {
                if (!_faction.IsStartingMember(faction, component.Faction))
                {
                    _faction.RemoveFaction(faction, component.Faction);
                }
                component.TimeFactionChangeBack = component.TimeFactionChange + component.TimeAsFaction + TimeSpan.FromSeconds(_random.Next(component.RandomBonusTimeAsFaction + 1));
            }
        }
    }

    private void OnTryRemoveFaction(Entity<NpcTimedFactionComponent> ent, ref NpcFactionSystem.TryRemoveFactionAttemptEvent args)
    {
        var beforeNextChange = ent.Comp.TimeFactionChange >= _timing.CurTime;
        var afterChangeBack = _timing.CurTime >= ent.Comp.TimeFactionChangeBack;
        if (ent.Comp.HasChangedOnce && args.Faction == ent.Comp.Faction && beforeNextChange && !afterChangeBack)
        {
            args.Cancel();
        }
    }
}
