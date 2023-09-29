using Content.Server.Mind;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Objectives;

public sealed class ObjectivesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public ObjectivePrototype? GetRandomObjective(EntityUid mindId, MindComponent mind, string objectiveGroupProto)
    {
        if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(objectiveGroupProto, out var groups))
        {
            Log.Error("Tried to get a random objective, but can't index WeightedRandomPrototype " + objectiveGroupProto);
            return null;
        }

        // TODO replace whatever the fuck this is with a proper objective selection system
        // yeah the old 'preventing infinite loops' thing wasn't super elegant either and it mislead people on what exactly it did
        var tries = 0;
        while (tries < 20)
        {
            var groupName = groups.Pick(_random);

            if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(groupName, out var group))
            {
                Log.Error("Couldn't index objective group prototype" + groupName);
                return null;
            }

            if (_prototypeManager.TryIndex<ObjectivePrototype>(group.Pick(_random), out var objective)
                && objective.CanBeAssigned(mindId, mind))
                return objective;
            else
                tries++;
        }

        return null;
    }
}
