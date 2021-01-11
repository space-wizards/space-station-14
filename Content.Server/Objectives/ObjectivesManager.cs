#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Objectives
{
    public class ObjectivesManager : IObjectivesManager
    {
        [Dependency] private IPrototypeManager _prototypeManager = default!;
        [Dependency] private IRobustRandom _random = default!;

        public IEnumerable<ObjectivePrototype> GetAllPossibleObjectives(Mind mind)
        {
            return _prototypeManager.EnumeratePrototypes<ObjectivePrototype>().Where(objectivePrototype => objectivePrototype.CanBeAssigned(mind));
        }

        public ObjectivePrototype? GetRandomObjective(Mind mind)
        {
            var objectives = GetAllPossibleObjectives(mind).ToList();
            _random.Shuffle(objectives);

            //to prevent endless loops
            foreach (var objective in objectives)
            {
                if (!_random.Prob(objective.Probability)) continue;
                return objective;
            }

            return null;
        }
    }
}
