using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Random;
using System;
using System.Linq;
using System.Collections.Generic;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Light.Components;
using Content.Server.Light.Components;

namespace Content.Shared.Plankton
{
    public sealed class PlanktonSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
    //    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default;

        private const float UpdateInterval = 1f; // Interval in seconds
        private const float HungerInterval = 5f;
        
        private float _updateTimer = 0f;
        private float _hungerTimer = 0f;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlanktonComponent, ComponentInit>(OnPlanktonCompInit);
        }

         public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _updateTimer += frameTime;
            _hungerTimer += frameTime;

            if (_updateTimer >= UpdateInterval)
            {
                foreach (var entity in EntityManager.EntityQuery<PlanktonComponent>())
                {
                     CheckPlanktonDiet(entity);
                     CheckPlanktonCharacteristics(entity);
                }
                _updateTimer = 0f;
            }

            if (_hungerTimer >= HungerInterval)
            {
                foreach (var entity in EntityManager.EntityQuery<PlanktonComponent>())
                {
                     PlanktonHunger(entity);
                }
                _hungerTimer = 0f;
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
                    50f,
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
            CheckPlanktonCharacteristics(component, uid);
            CheckPlanktonDiet(component, uid);
        }

       private void CheckPlanktonCharacteristics(PlanktonComponent component, EntityUid uid)
        {
    foreach (var planktonInstance in component.SpeciesInstances)
    {
        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Aggressive) != 0)
        {
            PerformAggressionCheck(uid);
        }

        if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Bioluminescent) != 0)
        {
            if (planktonInstance.IsAlive == true)
            {
                EntityManager.EnsureComponent<PointLightComponent>(uid);
                Log.Info($"{planktonInstance.SpeciesName} is actively glowing.")
            }
            else
            {
                _entityManager.RemoveComponent<PointLightComponent>(uid);
            }
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

        private void CheckPlanktonDiet(PlanktonComponent component, EntityUid uid)
        {
            foreach (var planktonInstance in component.SpeciesInstances)
            {
                if ((planktonInstance.Diet & PlanktonComponent.PlanktonDiet.Decomposer) != 0)
                {
                    if (planktonInstance.IsAlive == true)
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
            planktonInstance.CurrentHunger += sizeGrowth;

            if (component.DeadPlankton < 0)
            {
                component.DeadPlankton = 0;
                Log.Info($"All dead plankton have been eaten.");
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

                 var carnivorousPlanktonInstances = component.SpeciesInstances
                    .Where(inst => (inst.Diet & PlanktonComponent.PlanktonDiet.Carnivore) != 0)
                    .ToList();

                if (aggressivePlanktonInstances.Any())
                {
                    foreach (var aggressivePlankton in aggressivePlanktonInstances)
                    {
                        if (aggressivePlankton.IsAlive == true)
                        {
                        foreach (var otherPlankton in component.SpeciesInstances)
                        {
                            if (aggressivePlankton == otherPlankton) continue;

                            ReducePlanktonSizeAggression(otherPlankton, component, aggressivePlankton);
                        }
                        }
                    }

                if (carnivorousPlanktonInstances.Any())
                {
                    foreach (var carnivorousPlankton in carnivorousPlanktonInstances)
                    {
                        if (carnivorousPlankton.IsAlive == true)
                        {
                        foreach (var otherPlankton in component.SpeciesInstances)
                        {
                            if (carnivorousPlankton == otherPlankton) continue;

                            ReducePlanktonSizeCarnivorous(otherPlankton, component, carnivorousPlankton);
                        }
                        }
                    }
                }
                }
            }
        }


        private void ReducePlanktonSizeCarnivorous(PlanktonComponent.PlanktonSpeciesInstance planktonInstance, PlanktonComponent component, PlanktonComponent.PlanktonSpeciesInstance carnivorousPlankton)
        {
            if (planktonInstance.IsAlive == true && carnivorousPlankton.IsAlive == true)
            {
             float food = 0.5f;
             planktonInstance.CurrentSize -= food;
             Log.Info($"Reduced size of {planktonInstance.SpeciesName} to {planktonInstance.CurrentSize} via being predated on by {carnivorousPlankton.SpeciesName}");
             carnivorousPlankton.CurrentHunger += food;

              if (planktonInstance.CurrentSize < 0)
              {
                planktonInstance.CurrentSize = 0;
                Log.Info($"{planktonInstance.SpeciesName} has been wiped out by {carnivorousPlankton.SpeciesName}.");
                planktonInstance.IsAlive = false;
                // change IsAlive once the framework is finished
              }
            }
        }

        private void ReducePlanktonSizeAggression(PlanktonComponent.PlanktonSpeciesInstance planktonInstance, PlanktonComponent component, PlanktonComponent.PlanktonSpeciesInstance aggressivePlankton)
        {
            if (planktonInstance.IsAlive == true && aggressivePlankton.IsAlive == true)
            {
             float sizeReduction = 0.1f;
             planktonInstance.CurrentSize -= sizeReduction;
             component.DeadPlankton += sizeReduction;
             Log.Info($"Reduced size of {planktonInstance.SpeciesName} to {planktonInstance.CurrentSize} via being aggressively attacked by {aggressivePlankton.SpeciesName} ");

              if (planktonInstance.CurrentSize < 0)
              {
                planktonInstance.CurrentSize = 0;
                Log.Info($"{planktonInstance.SpeciesName} has been wiped out by {aggressivePlankton.SpeciesName}.");
                planktonInstance.IsAlive = false;
                // change IsAlive once the framework is finished
              }
            }
        }

         private void PlanktonHunger(PlanktonComponent component)
        {
            foreach (var planktonInstance in component.SpeciesInstances)
            {
            if (planktonInstance.IsAlive == true)
            {
                if (planktonInstance.CurrentHunger <= 50f)
                {
                    // If hunger is 0 or less, plankton dies
                    if (planktonInstance.CurrentHunger <= 0f)
                    {
                        planktonInstance.CurrentHunger = 0f;
                        planktonInstance.IsAlive = false;
                        Log.Error($"{planktonInstance.SpeciesName} has starved to death.");
                    }
                }
                else
                {
                    // Reduce hunger if greater than 0
                    var HungerLoss = 0.5f;
                    planktonInstance.CurrentHunger -= HungerLoss;
                    planktonInstance.CurrentHunger = Math.Max(0f, planktonInstance.CurrentHunger);  // Ensure it doesn't go below 0

                    Log.Error($"{planktonInstance.SpeciesName} has lost {HungerLoss} hunger. It is now at {planktonInstance.CurrentHunger}");
                    }

            // Ensure hunger doesn't exceed 50 and log if full
            if (planktonInstance.CurrentHunger >= 50f)
            {
                planktonInstance.CurrentHunger = 50f;  // Clamp the hunger to a maximum of 50
                Log.Error($"{planktonInstance.SpeciesName} is full.");
            }
        }
    }
}


    }
}

