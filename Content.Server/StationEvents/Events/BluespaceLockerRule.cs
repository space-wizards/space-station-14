using Content.Server.Resist;
using Content.Server.StationEvents.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Station.Components;
using Content.Shared.Storage.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events bluespace linking two lockers together (teleporting between them on close).
/// </summary>
/// <seealso cref="BluespaceLockerRuleComponent"/>
/// <seealso cref="BluespaceLockerComponent"/>
public sealed partial class BluespaceLockerRule : StationEventSystem<BluespaceLockerRuleComponent>
{
    [Dependency] private BluespaceLockerSystem _bluespaceLocker = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Started(Entity<BluespaceLockerRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var targets = new List<EntityUid>();
        var query = EntityQueryEnumerator<EntityStorageComponent, ResistLockerComponent>();
        while (query.MoveNext(out var storageUid, out _, out _))
        {
            targets.Add(storageUid);
        }

        RobustRandom.Shuffle(targets);
        foreach (var potentialLink in targets)
        {
            if (HasComp<AccessReaderComponent>(potentialLink) ||
                HasComp<BluespaceLockerComponent>(potentialLink) ||
                !HasComp<StationMemberComponent>(_transform.GetGrid(potentialLink)))
                continue;

            var comp = AddComp<BluespaceLockerComponent>(potentialLink);

            comp.PickLinksFromSameMap = true;
            comp.MinBluespaceLinks = 1;
            comp.BehaviorProperties.BluespaceEffectOnTeleportSource = true;
            comp.AutoLinksBidirectional = true;
            comp.AutoLinksUseProperties = true;
            comp.AutoLinkProperties.BluespaceEffectOnInit = true;
            comp.AutoLinkProperties.BluespaceEffectOnTeleportSource = true;
            _bluespaceLocker.GetTarget(potentialLink, comp, true);
            _bluespaceLocker.BluespaceEffect(potentialLink, comp, comp, true);

            Sawmill.Info($"Converted {ToPrettyString(potentialLink)} to bluespace locker");

            return;
        }
    }
}
