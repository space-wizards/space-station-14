using Content.Server.StationEvents.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Traits.Assorted;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that give humanoids paracusia, causing them to hear strange sounds.
/// </summary>
/// <seealso cref="MassHallucinationsRuleComponent"/>
/// <seealso cref="ParacusiaComponent"/>
public sealed partial class MassHallucinationsRule : StationEventSystem<MassHallucinationsRuleComponent>
{
    [Dependency] private ParacusiaSystem _paracusia = default!;

    protected override void Started(Entity<MassHallucinationsRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var hallucinations = ent.Comp1;

        var query = EntityQueryEnumerator<MindContainerComponent, HumanoidProfileComponent>();
        while (query.MoveNext(out var victim, out _, out _))
        {
            if (!EnsureComp<ParacusiaComponent>(victim, out var paracusia))
            {
                _paracusia.SetSounds(victim, hallucinations.Sounds, paracusia);
                _paracusia.SetTime(victim, hallucinations.MinTimeBetweenIncidents, hallucinations.MaxTimeBetweenIncidents, paracusia);
                _paracusia.SetDistance(victim, hallucinations.MaxSoundDistance);

                hallucinations.AffectedEntities.Add(victim);
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
