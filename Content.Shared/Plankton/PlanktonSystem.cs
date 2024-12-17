using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Random;
using System;
using System.Linq;
using System.Collections.Generic;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.Plankton
{
    public sealed class PlanktonSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
    //    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default;

        private const float UpdateInterval = 1f; // Interval in seconds
        private float _updateTimer = 0f;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlanktonComponent, ComponentInit>(OnPlanktonCompInit);
        }

         public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _updateTimer += frameTime;

            // If it's time for the next update
            if (_updateTimer >= UpdateInterval)
            {
            foreach (var entity in EntityManager.EntityQuery<PlanktonComponent>())
            {
             PerformAggressionCheck(entity);  // Pass EntityUid instead of the component
             CheckPlanktonDiet(entity);
             CheckPlanktonCharacteristics(entity);
             PlanktonHunger(entity);
            }



                _updateTimer = 0f;
            }
        }

        private void OnPlanktonCompInit(EntityUid uid, PlanktonComponent component, ComponentInit args)
        {
            var random = new System.Random();
          //  var reagentId = reagentId.Prototype;

           // component.ReagentId = reagentId;

            //if (reagentId != SeaWater)
           // {
            //    component.IsAlive = false;
            //    Log.Error("The plankton component died due to an invalid environment.");
           //     return;
          //  }

            for (int i = 0; i < 3; i++)
            {
                var firstName = PlanktonComponent.PlanktonFirstNames[random.Next(PlanktonComponent.PlanktonFirstNames.Length)];
                var secondName = PlanktonComponent.PlanktonSecondNames[random.Next(PlanktonComponent.PlanktonSecondNames.Length)];
                var planktonName = new PlanktonComponent.PlanktonName(firstName, secondName);

                // Randomly generate 2-3 characteristics per plankton
                int numCharacteristics = random.Next(2, 4);  // Randomly pick 2-3 characteristics
                var possibleCharacteristics = Enum.GetValues(typeof(PlanktonComponent.PlanktonCharacteristics));
                var selectedCharacteristics = new HashSet<PlanktonComponent.PlanktonCharacteristics>();

               while (selectedCharacteristics.Count < numCharacteristics)
                {
                    var characteristicValue = possibleCharacteristics.GetValue(random.Next(possibleCharacteristics.Length));
                 if (characteristicValue != null)
                 {
                        var randomCharacteristic = (PlanktonComponent.PlanktonCharacteristics)characteristicValue;
                        if ((selectedCharacteristics.Contains(PlanktonComponent.PlanktonCharacteristics.Cryophilic) &&
                            selectedCharacteristics.Contains(PlanktonComponent.PlanktonCharacteristics.Pyrophilic)) ||
                           (randomCharacteristic == PlanktonComponent.PlanktonCharacteristics.Cryophilic &&
                          selectedCharacteristics.Contains(PlanktonComponent.PlanktonCharacteristics.Pyrophilic)) ||
                         (randomCharacteristic == PlanktonComponent.PlanktonCharacteristics.Pyrophilic &&
                          selectedCharacteristics.Contains(PlanktonComponent.PlanktonCharacteristics.Cryophilic)))
                       {
                         Log.Error("Disallowed characteristic mix: Cryophilic and Pyrophilic cannot coexist.");
                            continue;
                       }
                       selectedCharacteristics.Add(randomCharacteristic);
                 }
}


                PlanktonComponent.PlanktonCharacteristics combinedCharacteristics = 0;
                foreach (var characteristic in selectedCharacteristics)
                {
                    combinedCharacteristics |= characteristic;
                }

                // Create a new plankton species instance
                var planktonInstance = new PlanktonComponent.PlanktonSpeciesInstance(
                    planktonName,
                    (PlanktonComponent.PlanktonDiet)random.Next(Enum.GetValues<PlanktonComponent.PlanktonDiet>().Length),
                    combinedCharacteristics,
                    1.0f,
                    100f,
                    true
                );

                // Add the plankton species instance to the SpeciesInstances list
                component.SpeciesInstances.Add(planktonInstance);

                Log.Info($"Generated plankton species {planktonInstance.SpeciesName} with characteristics {combinedCharacteristics} and diet {planktonInstance.Diet}");
            }

            // Log the total number of plankton species initialized
            Log.Info($"Plankton component initialized with {component.SpeciesInstances.Count} species.");

            PlanktonInteraction(uid);
        }

        private void PlanktonInteraction(EntityUid uid)
        {
            if (!HasComp<PlanktonComponent>(uid))
            {
                Log.Error($"No PlanktonComponent found for entity {uid}");
                return;
            }

            var component = _entityManager.GetComponent<PlanktonComponent>(uid);
            CheckPlanktonCharacteristics(component);
            CheckPlanktonDiet(component);
        }

       private void CheckPlanktonCharacteristics(PlanktonComponent component)
        {
    foreach (var planktonInstance in component.SpeciesInstances)
    {
        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Aggressive) != 0)
        {

        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Bioluminescent) != 0)
        {

        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Mimicry) != 0)
        {

        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.ChemicalProduction) != 0)
        {

        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.MagneticField) != 0)
        {
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Hallucinogenic) != 0)
        {
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.PheromoneGlands) != 0)
        {
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.PolypColony) != 0)
        {
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.AerosolSpores) != 0)
        {
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.HyperExoticSpecies) != 0)
        {
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Sentience) != 0)
        {
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Pyrophilic) != 0)
        {
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Cryophilic) != 0)
        {
        }
    }
}

        private void CheckPlanktonDiet(PlanktonComponent component)
        {
            foreach (var planktonInstance in component.SpeciesInstances)
            {
                if ((planktonInstance.Diet & PlanktonComponent.PlanktonDiet.Decomposer) != 0)
                {
                    if (planktonInstance.IsAlive = true)
                    {
                        Log.Info($"{planktonInstance.SpeciesName} is alive and a decomposer!");
                        if (component.DeadPlankton > 0)
                         {
                                DecomposeCheck(planktonInstance, component);
                         }
                    }

                }
            }
        }

         private void DecomposeCheck(PlanktonComponent.PlanktonSpeciesInstance planktonInstance, PlanktonComponent component)
        {
            float sizeGrowth = 0.1f;
            component.DeadPlankton -= sizeGrowth;
            // add satiation once made

            if (component.DeadPlankton < 0)
            {
                component.DeadPlankton = 0;
                Log.Info($"All dead plankton have been eaten.");
                // change IsAlive once the framework is finished
            }
            planktonInstance.CurrentHunger += sizeGrowth;
            Log.Info($"Increased satiation of {planktonInstance.SpeciesName} to {planktonInstance.CurrentHunger} from decomposing food. There is {component.DeadPlankton} food left.");

        }

        private void PerformAggressionCheck(PlanktonComponent component)
        {
            foreach (var planktonEntity in EntityManager.EntityQuery<PlanktonComponent>())
            {

                var aggressivePlanktonInstances = component.SpeciesInstances
                    .Where(inst => (inst.Characteristics & PlanktonComponent.PlanktonCharacteristics.Aggressive) != 0)
                    .ToList();

                if (aggressivePlanktonInstances.Any()) // add IsAlive framework here too
                {
                    foreach (var aggressivePlankton in aggressivePlanktonInstances)
                    {
                        foreach (var otherPlankton in component.SpeciesInstances)
                        {
                            if (aggressivePlankton == otherPlankton) continue;

                            ReducePlanktonSize(otherPlankton, component);
                        }
                    }
                }
            }
        }

        private void ReducePlanktonSize(PlanktonComponent.PlanktonSpeciesInstance planktonInstance, PlanktonComponent component)
        {
            if (planktonInstance.IsAlive = true)
            {
             float sizeReduction = 0.1f;
             planktonInstance.CurrentSize -= sizeReduction;
             component.DeadPlankton += sizeReduction;
             Log.Info($"Reduced size of {planktonInstance.SpeciesName} to {planktonInstance.CurrentSize}");

              if (planktonInstance.CurrentSize < 0)
              {
                planktonInstance.CurrentSize = 0;
                Log.Info($"{planktonInstance.SpeciesName} has been wiped out.");
                planktonInstance.IsAlive = false;
                // change IsAlive once the framework is finished
              }
            }
        }

         private void PlanktonHunger(PlanktonComponent component)
        {
            foreach (var planktonInstance in component.SpeciesInstances)
            {
                if (planktonInstance.IsAlive = true)
                {
                    if (planktonInstance.CurrentHunger > 0)
                    {
                      var HungerLoss = 10f;
                      planktonInstance.CurrentHunger -= HungerLoss;
                      Log.Error($"{planktonInstance.SpeciesName} has lost 10 hunger. It is now at {planktonInstance.CurrentHunger}");
                      if (planktonInstance.CurrentHunger < 0)
                      {
                         planktonInstance.CurrentHunger = 0f;
                         planktonInstance.IsAlive = false;
                      }
                   }
                }
            }
        }

    }
}

