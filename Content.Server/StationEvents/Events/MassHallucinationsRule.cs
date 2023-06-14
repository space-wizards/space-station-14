using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio;

namespace Content.Server.StationEvents.Events;

public sealed class MassHallucinationsRule : StationEventSystem<MassHallucinationsRuleComponent>
{
    [Dependency] private readonly ParacusiaSystem _paracusia = default!;

    protected override void Started(EntityUid uid, MassHallucinationsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        var query = EntityQueryEnumerator<MindComponent>();
        var sounds = new SoundCollectionSpecifier("Paracusia");
        while (query.MoveNext(out var ent, out _))
        {
            if (!HasComp<ParacusiaComponent>(ent))
            {
                EnsureComp<MassHallucinationsComponent>(ent);
                var paracusia = EnsureComp<ParacusiaComponent>(ent);
                _paracusia.SetSounds(ent, sounds, paracusia);
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
