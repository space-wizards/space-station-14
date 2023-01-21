using System.Linq;
using Content.Server.Resist;
using Content.Server.Station.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Coordinates;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class BluespaceLockerLink : StationEventSystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly BluespaceLockerSystem _bluespaceLocker = default!;

    public override string Prototype => "BluespaceLockerLink";

    public override void Started()
    {
        base.Started();

        var targets = EntityQuery<EntityStorageComponent, ResistLockerComponent>().ToList();
        _robustRandom.Shuffle(targets);

        foreach (var target in targets)
        {
            var potentialLink = target.Item1.Owner;

            if (HasComp<AccessReaderComponent>(potentialLink) ||
                HasComp<BluespaceLockerComponent>(potentialLink) ||
                !HasComp<StationMemberComponent>(potentialLink.ToCoordinates().GetGridUid(EntityManager)))
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
