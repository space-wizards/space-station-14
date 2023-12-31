using Content.Server.GameTicking.Rules.Components;
using Content.Server.NPC.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.Imperial.ICCVar;
using Content.Shared.Mind.Components;
using Content.Shared.Traits.Assorted;
using FastAccessors;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class MassPsychosisRule : StationEventSystem<MassPsychosisRuleComponent>
{
    [Dependency] private readonly PsychosisSystem _psychosis = default!;

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    protected override void Started(EntityUid uid, MassPsychosisRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (_cfg.GetCVar(ICCVars.PsychosisEnabled) == false)
            return;
        base.Started(uid, component, gameRule, args);
        var query = EntityQueryEnumerator<PsychosisGainComponent>();
        var list = new List<EntityUid>();
        while (query.MoveNext(out var uidthing, out var gain))
        {
            list.Add(uidthing);
        }
        var pickedlist = new List<EntityUid>();
        for (var str = 0; str < _random.Next(component.From, component.To); str++)
            pickedlist.Add(_random.PickAndTake(list));
        foreach (var entuid in pickedlist)
        {
            if (TryComp<PsychosisComponent>(entuid, out var psychosis))
                continue;
            if (TryComp<NpcFactionMemberComponent>(entuid, out var faction))
            {
                foreach (var fact in faction.Factions)
                {
                    if (fact == "Zombie")
                        continue;
                }
            }
            AddComp<PsychosisComponent>(entuid);
        }
    }
}

