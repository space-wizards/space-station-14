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

        public List<ObjectivePrototype> GetAllPossibleObjectives(Mind mind)
        {
            return _prototypeManager.EnumeratePrototypes<ObjectivePrototype>().Where(objectivePrototype => objectivePrototype.CanBeAssigned(mind)).ToList();
        }

        public ObjectivePrototype[] GetRandomObjectives(Mind mind, float maxDifficulty)
        {
            var objectives = GetAllPossibleObjectives(mind);

            //to prevent endless loops
            if(objectives.Sum(o => o.Difficulty) == 0f) return objectives.ToArray();

            var result = new List<ObjectivePrototype>();
            var currentDifficulty = 0f;
            _random.Shuffle(objectives);
            while (currentDifficulty < maxDifficulty && objectives.Count > 0)
            {
                var incompatible = new List<ObjectivePrototype>();
                foreach (var objective in objectives)
                {
                    if (!objective.IsCompatible(result))
                    {
                        incompatible.Add(objective);
                        continue;
                    }
                    if (!_random.Prob(objective.Probability)) continue;

                    result.Add(objective);
                    currentDifficulty += objective.Difficulty;
                    if (currentDifficulty >= maxDifficulty) break;
                }

                foreach (var objectivePrototype in incompatible)
                {
                    objectives.Remove(objectivePrototype);
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
