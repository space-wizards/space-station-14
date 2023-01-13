using System.Linq;
using Content.Server.Resist;
using Content.Server.Station.Components;
using Content.Server.Storage.Components;
using Content.Shared.Access.Components;
using Content.Shared.Coordinates;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class BluespaceLockerLink : StationEventSystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

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

            using var compInitializeHandle = EntityManager.AddComponentUninitialized<BluespaceLockerComponent>(potentialLink);
            var comp = compInitializeHandle.Comp;

            comp.PickLinksFromSameMap = true;
            comp.MinBluespaceLinks = 1;
            comp.BluespaceEffectOnInit = true;
            comp.BehaviorProperties.BluespaceEffectOnTeleportSource = true;
            comp.AutoLinksBidirectional = true;
            comp.AutoLinksUseProperties = true;
            comp.AutoLinkProperties.BluespaceEffectOnTeleportSource = true;

            compInitializeHandle.Dispose();

            Sawmill.Info($"Converted {ToPrettyString(potentialLink)} to bluespace locker");

            return;
        }
    }
}
