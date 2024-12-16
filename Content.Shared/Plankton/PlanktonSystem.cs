using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Random;
using System;
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
                PerformAggressionCheck(); // move this to aggressive instead of it's own field on Update
                CheckPlanktonDiet();
                CheckPlanktonCharacteristics();

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
                    1.0f
                );

                // Add the plankton species instance to the SpeciesInstances list
                component.SpeciesInstances.Add(planktonInstance);

                Log.Info($"Generated plankton species {planktonInstance.SpeciesName} with characteristics {combinedCharacteristics}");
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
            Log.Error($"{planktonInstance.SpeciesName} is aggressive");

        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Bioluminescent) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} is bioluminescent");
      
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Mimicry) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} is a mimic");
  
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.ChemicalProduction) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} produces chemicals");
  
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.MagneticField) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} produces a magnetic field");
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Hallucinogenic) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} makes you high");
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.PheromoneGlands) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} produces pheromones");
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.PolypColony) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} forms a polyp colony");
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.AerosolSpores) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} produces spores");
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.HyperExoticSpecies) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} is hyper-exotic");
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Sentience) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} is sentient");
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Pyrophilic) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} is happiest in heat");
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Cryophilic) != 0)
        {
            Log.Info($"{planktonInstance.SpeciesName} is happiest in cold");
        }
    }
}

        private void CheckPlanktonDiet(PlanktonComponent component)
        {
            foreach (var planktonInstance in component.SpeciesInstances)
            {
                if ((planktonInstance.Diet & PlanktonComponent.PlanktonDiet.Decomposer) != 0)
                {
                    Log.Error($"{planktonInstance.SpeciesName} eats other species");
                    if (component.DeadPlankton > 0)
                    {
                        DecomposeCheck(planktonInstance, component);
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
            planktonInstance.CurrentSize += sizeGrowth;
            Log.Info($"Increased size of {planktonInstance.SpeciesName} to {planktonInstance.CurrentSize} from decomposing food."};
            
        }
        
        private void PerformAggressionCheck()
        {
            foreach (var planktonEntity in EntityManager.EntityQuery<PlanktonComponent>())
            {
                var component = planktonEntity.Value;

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

                            ReducePlanktonSize(otherPlankton);
                        }
                    }
                }
            }
        }

        private void ReducePlanktonSize(PlanktonComponent.PlanktonSpeciesInstance planktonInstance, PlanktonComponent component)
        {
            float sizeReduction = 0.1f;
            planktonInstance.CurrentSize -= sizeReduction;
            component.DeadPlankton += sizeReduction;

            if (planktonInstance.CurrentSize < 0)
            {
                planktonInstance.CurrentSize = 0;
                Log.Info($"{planktonInstance.SpeciesName} has been wiped out.");
                // change IsAlive once the framework is finished
            }

            Log.Info($"Reduced size of {planktonInstance.SpeciesName} to {planktonInstance.CurrentSize}");
        }
    }
}
