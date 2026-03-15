using Content.Server.StationEvents.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Traits.Assorted;


namespace Content.Server.StationEvents.Events;

public sealed class MassHallucinationsRule : StationEventSystem<MassHallucinationsRuleComponent>
{
    [Dependency] private readonly ParacusiaSystem _paracusia = default!;

    protected override void Started(EntityUid uid, MassHallucinationsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var query = EntityQueryEnumerator<MindContainerComponent, HumanoidProfileComponent>();
        while (query.MoveNext(out var ent, out _, out _))
        {
            // People who already have paracusia (usually via trait) strangely don't hear mass hallucinations :)
            if (EnsureComp<ParacusiaComponent>(ent, out var paracusia))
                continue;
            _paracusia.SetSounds((ent, paracusia), component.Sounds);
            _paracusia.SetTime((ent, paracusia), component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents);
            _paracusia.SetDistance((ent, paracusia), component.MaxSoundDistance);

            component.AffectedEntities.Add(ent);
        }
    }

    protected override void Ended(EntityUid uid, MassHallucinationsRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        foreach (var ent in component.AffectedEntities)
        {
            RemComp<ParacusiaComponent>(ent);
        }

        component.AffectedEntities.Clear();
    }
}
