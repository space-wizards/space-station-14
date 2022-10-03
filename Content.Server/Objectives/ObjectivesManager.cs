using Content.Server.Objectives.Interfaces;
using Content.Shared.Random.Helpers;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Objectives
{
    public sealed class ObjectivesManager : IObjectivesManager
    {
        [Dependency] private IPrototypeManager _prototypeManager = default!;
        [Dependency] private IRobustRandom _random = default!;

        public ObjectivePrototype? GetRandomObjective(Mind.Mind mind, string objectiveGroupProto)
        {
            if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(objectiveGroupProto, out var groups))
            {
                Logger.Error("Tried to get a random objective, but can't index WeightedRandomPrototype " + objectiveGroupProto);
                return null;
            }

            // yeah the old 'preventing infinite loops' thing wasn't super elegant either and it mislead people on what exactly it did
            var tries = 0;
            while (tries < 20)
            {
                var groupName = groups.Pick(_random);

                if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(groupName, out var group))
                {
                    Logger.Error("Couldn't index objective group prototype" + groupName);
                    return null;
                }

                if (_prototypeManager.TryIndex<ObjectivePrototype>(group.Pick(_random), out var objective)
                    && objective.CanBeAssigned(mind))
                    return objective;
                else
                    tries++;
            }

            return null;
        }
    }
}
