using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Collections;

namespace Content.Server.StationEvents.Events;

public sealed class MassHallucinationsRule : StationEventSystem<MassHallucinationsRuleComponent>
{
    [Dependency] private readonly ParacusiaSystem _paracusia = default!;

    private ValueList<EntityUid> _toChange = new();

    protected override void Started(EntityUid uid, MassHallucinationsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {

        base.Started(uid, component, gameRule, args);

        _toChange.Clear();
        var query = EntityQueryEnumerator<MindContainerComponent, HumanoidAppearanceComponent>();
        while (query.MoveNext(out var ent, out _, out _))
            if (!HasComp<ParacusiaComponent>(ent))
                _toChange.Add(ent);

        foreach (EntityUid ent in _toChange)
        {
            EnsureComp<MassHallucinationsComponent>(ent);
            var paracusia = EnsureComp<ParacusiaComponent>(ent);
            _paracusia.SetSounds(ent, component.Sounds, paracusia);
            _paracusia.SetTime(ent, component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents, paracusia);
            _paracusia.SetDistance(ent, component.MaxSoundDistance);
        }
    }

    protected override void Ended(EntityUid uid, MassHallucinationsRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        foreach (EntityUid ent in _toChange)
        {
            RemComp<ParacusiaComponent>(ent);
            RemComp<MassHallucinationsComponent>(ent);
        }
    }
}
