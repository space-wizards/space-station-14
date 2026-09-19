using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Traits.Assorted;


namespace Content.Server.StationEvents.Events;

public sealed partial class MassHallucinationsRule : StationEventSystem<MassHallucinationsRuleComponent>
{
    [Dependency] private ParacusiaSystem _paracusia = default!;

    protected override void Started(EntityUid uid, MassHallucinationsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var query = EntityQueryEnumerator<MindContainerComponent, HumanoidProfileComponent>();
        while (query.MoveNext(out var ent, out _, out _))
        {
            if (!EnsureComp<ParacusiaComponent>(ent, out var paracusia))
            {
                _paracusia.SetSounds(ent, component.Sounds, paracusia);
                _paracusia.SetTime(ent, component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents, paracusia);
                _paracusia.SetDistance(ent, component.MaxSoundDistance);

                component.AffectedEntities.Add(ent);
            }
        }
    }

    protected override void Ended(Entity<MassHallucinationsRuleComponent> rule, ref GameRuleEndedEvent args)
    {
        base.Ended(rule, ref args);

        foreach (var ent in rule.Comp.AffectedEntities)
        {
            RemComp<ParacusiaComponent>(ent);
        }

        rule.Comp.AffectedEntities.Clear();
    }
}
