using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.Mind.Components;
using Content.Shared.Traits.Assorted;

namespace Content.Server.StationEvents.Events;

public sealed class MassHallucinationsRule : StationEventSystem<MassHallucinationsRuleComponent>
{
    [Dependency] private readonly ParacusiaSystem _paracusia = default!;

    protected override void Started(EntityUid uid, MassHallucinationsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        var query = EntityQueryEnumerator<MindContainerComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            if (!HasComp<ParacusiaComponent>(ent))
            {
                EnsureComp<MassHallucinationsComponent>(ent);
                var paracusia = EnsureComp<ParacusiaComponent>(ent);
                _paracusia.SetSounds(ent, component.Sounds, paracusia);
                _paracusia.SetTime(ent, component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents, paracusia);
                _paracusia.SetDistance(ent, component.MaxSoundDistance);
            }
        }
    }

    protected override void Ended(EntityUid uid, MassHallucinationsRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
        var query = EntityQueryEnumerator<MassHallucinationsComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            RemComp<ParacusiaComponent>(ent);
        }
    }
}
