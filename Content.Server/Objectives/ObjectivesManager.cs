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

        public ObjectivePrototype[] GetAllPossibleObjectives(Mind mind)
        {
            return _prototypeManager.EnumeratePrototypes<ObjectivePrototype>().Where(objectivePrototype => objectivePrototype.CanBeAssigned(mind)).ToArray();
        }

        public ObjectivePrototype[] GetRandomObjectives(Mind mind, float maxDifficulty = 3)
        {
            var objectives = GetAllPossibleObjectives(mind);

            //to prevent endless loops
            if(objectives.Length == 0 || objectives.Sum(o => o.Difficulty) == 0f) return objectives;

            var result = new List<ObjectivePrototype>();
            var currentDifficulty = 0f;
            _random.Shuffle(objectives);
            while (currentDifficulty < maxDifficulty)
            {
                foreach (var objective in objectives)
                {
                    if (!_random.Prob(objective.Probability)) continue;

                    result.Add(objective);
                    currentDifficulty += objective.Difficulty;
                    if (currentDifficulty >= maxDifficulty) break;
                }
            }

            if (currentDifficulty > maxDifficulty) //will almost always happen
            {
                result.Pop();
            }

            return result.ToArray();
        }
    }
}
