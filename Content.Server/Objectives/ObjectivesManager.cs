using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Interfaces.GameObjects;
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

        public ObjectivePrototype[] GetAllPossibleObjectives(IEntity entity)
        {
            return _prototypeManager.EnumeratePrototypes<ObjectivePrototype>().Where(objectivePrototype => objectivePrototype.CanBeAssigned(entity)).ToArray();
        }

        public ObjectivePrototype[] GetRandomObjectives(IEntity entity, float maxDifficulty = 3)
        {
            var objectives = GetAllPossibleObjectives(entity);

            //to prevent endless loops
            if(objectives.Length == 0 || objectives.Sum(o => o.Difficulty) == 0f) return objectives;

            var result = new List<ObjectivePrototype>();
            var currentDifficulty = 0f;
            while (currentDifficulty < maxDifficulty)
            {
                _random.Shuffle(objectives);
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


        public void AssignObjectives(IEntity entity, ObjectivePrototype[] objectives)
        {
            throw new System.NotImplementedException();
        }
    }
}
