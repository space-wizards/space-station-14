using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Traits.Assorted;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.StationEvents.Events;

public sealed class MassHallucinationsRule : StationEventSystem<MassHallucinationsRuleComponent>
{
    [Dependency] private readonly ParacusiaSystem _paracusia = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    protected override void Started(EntityUid uid, MassHallucinationsRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        var query = EntityQueryEnumerator<MindContainerComponent>();

        if (component.sweetwaterOnly)
        {
            foreach (var grid in _mapManager.GetAllGrids().OrderBy(o => o.Owner))
            {
                var map = grid.Owner;
                if (TryComp(map, out SweetwaterComponent? gridXform))
                {
                    ApplyOceanSound(map, component);
                }
            }
        }
        else
        {
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
    }

    private void ApplyOceanSound(EntityUid map, MassHallucinationsRuleComponent component)
    {
        foreach (var ent in GetGridChildren(map))
        {
            if (HasComp<MindContainerComponent>(ent) && !HasComp<ParacusiaComponent>(ent))
            {
                EnsureComp<MassHallucinationsComponent>(ent);
                var paracusia = EnsureComp<ParacusiaComponent>(ent);
                _paracusia.SetSounds(ent, component.Sounds, paracusia);
                _paracusia.SetTime(ent, component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents, paracusia);
                _paracusia.SetDistance(ent, component.MaxSoundDistance);
            }
        }
    }

    private IEnumerable<EntityUid> GetGridChildren(EntityUid target)
    {
        if (TryComp<StationDataComponent>(target, out var station))
        {
            foreach (var grid in station.Grids)
            {
                var enumerator = Transform(grid).ChildEnumerator;
                while (enumerator.MoveNext(out var ent))
                {
                    yield return ent;
                }
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
