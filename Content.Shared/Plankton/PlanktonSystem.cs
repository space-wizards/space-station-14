using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;
using System;
using System.Linq;
using System.Collections.Generic;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Light.Components;
using Content.Server.Light.Components;
using Content.Shared.Verbs;
using Content.Shared.Radiation.Events;

namespace Content.Shared.Plankton
{
    public sealed class PlanktonSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
    //  [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default;

        private const float UpdateInterval = 1f; // Interval in seconds
        private const float HungerInterval = 5f;

        private float _updateTimer = 0f;
        private float _hungerTimer = 0f;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlanktonComponent, ComponentInit>(OnPlanktonCompInit);
            SubscribeLocalEvent<PlanktonComponent, GetVerbsEvent<ActivationVerb>>(TogglePlanktonGeneration);
            SubscribeLocalEvent<PlanktonComponent, OnIrradiatedEvent>(OnRadiation);
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
                    EntityUid uid = entity.Owner;  // entity.Owner is the EntityUid
                    PlanktonInteraction(uid);
                }
                _updateTimer = 0f;
            }

            if (_hungerTimer >= HungerInterval)
            {
                foreach (var entity in EntityManager.EntityQuery<PlanktonComponent>())
                {
                     PlanktonHunger(entity);
                     PlanktonGrowth(entity);
                }
                _hungerTimer = 0f;
            }
        }

        private void OnPlanktonCompInit(EntityUid uid, PlanktonComponent component, ComponentInit args)
        {
            Log.Info($"Plankton component initialized");
        }

        private void TogglePlanktonGeneration(EntityUid uid, PlanktonComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        ActivationVerb verb = new()
        {
            Text = Loc.GetString("toggle-plankton-generation-verb"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () => PlanktonGeneration(uid, component),
            Priority = -1
        };

        args.Verbs.Add(verb);
    }

        private void PlanktonGeneration(EntityUid uid, PlanktonComponent component)
        {
            var random = new System.Random();
          //  var reagentId = reagentId.Prototype;

           // component.ReagentId = reagentId;
                        // Make this a check that happens with the rest of the characteristic checks, and include solution fraction so some chems can be introduced but not a ton of them.
            //if (reagentId != SeaWater)
           // {
            //    component.IsAlive = false;
            //    Log.Error($"The Plankton died due to not being immersed in Seawater");
           //     return;
          //  }

            //  var reagentId = solution.GetPrimaryReagentId(uid);

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

                // confirms full hunger and alive status is sucessfully applied upon generation. The instance dislikes doing it itself.
                planktonInstance.IsAlive = true;
                planktonInstance.CurrentHunger = 50f;

                Log.Info($"Generated plankton species {planktonInstance.SpeciesName} with characteristics {combinedCharacteristics} and diet {planktonInstance.Diet}.");
            }

    // Log the total number of plankton species initialized
    Log.Info($"Plankton component initialized with {component.SpeciesInstances.Count} species.");
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
        if (planktonInstance.IsAlive)
        {
                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Aggressive) != 0)
                {
                    PerformAggressionCheck(component);
                }

                if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Bioluminescent) != 0)
                {
                   // if (planktonInstance.IsAlive == true)
                   // {
                  //      EntityManager.EnsureComponent<PointLightComponent>(uid);
                  //      Log.Info($"{planktonInstance.SpeciesName} is actively glowing.");
                   // }
                   // else
                 //   {
                  //      _entityManager.RemoveComponent<PointLightComponent>(uid);
                  //  }
                }

                 if ((planktonInstance.Characteristics & PlanktonComponent.PlanktonCharacteristics.Charged) != 0)
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
}


        private void OnRadiation(EntityUid uid, PlanktonComponent component, OnIrradiatedEvent args)
        {
             foreach (var planktonInstance in component.SpeciesInstances)
            {
                if (planktonInstance.Diet == PlanktonComponent.PlanktonDiet.Radiophage && planktonInstance.IsAlive)
                {

                        if (planktonInstance.CurrentHunger >= 50f)
                        {
                            continue;
                        }
                        else
                        {
                            planktonInstance.CurrentHunger += 0.5;
                            Log.Info("{planktonInstance.SpeciesName} is increasing satiation to {planktonInstance.CurrentHunger} from consuming radiation);
                        }
                }
                else
                {
                    planktonInstance.CurrentSize -= 0.5;
                    component.DeadPlankton += 0.5;
                    Log.Info($"{planktonInstance.SpeciesName} is dying due to radiation exposure! Current size is {planktonInstance.CurrentSize})

                    if (planktonInstance.CurrentSize <= 0)
                    {
                        planktonInstance.IsAlive = false;
                        Log.Info($"{planktonInstance.SpeciesName} has been killed by excess radiation exposure")
                    }
                }

            }
        }

        private void CheckPlanktonDiet(PlanktonComponent component, EntityUid uid)
        {
            foreach (var planktonInstance in component.SpeciesInstances)
            {
                if (planktonInstance.IsAlive)
                {

                        if (planktonInstance.Diet == PlanktonComponent.PlanktonDiet.Decomposer)
                        {
                                if (component.DeadPlankton > 0)
                                 {
                                    DecomposeCheck(planktonInstance, component);
                                 }
                        }

                        if (planktonInstance.Diet == PlanktonComponent.PlanktonDiet.Carnivore)
                        {
                                PerformCarnivoreCheck(component);
                        }
                }
            }
        }

         private void DecomposeCheck(PlanktonComponent.PlanktonSpeciesInstance planktonInstance, PlanktonComponent component)
        {
            float sizeGrowth = 0.2f;
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

    // Collect all plankton instances that need to be deleted
    var planktonToRemove = new List<PlanktonComponent.PlanktonSpeciesInstance>();

    foreach (var planktonEntity in EntityManager.EntityQuery<PlanktonComponent>())
    {
        var aggressivePlanktonInstances = component.SpeciesInstances
            .Where(inst => (inst.Characteristics & PlanktonComponent.PlanktonCharacteristics.Aggressive) != 0)
            .ToList();

        if (aggressivePlanktonInstances.Any())
        {
            foreach (var aggressivePlankton in aggressivePlanktonInstances)
            {
                foreach (var otherPlankton in component.SpeciesInstances)
                {
                    if (aggressivePlankton == otherPlankton) continue;

                    if (otherPlankton.IsAlive && aggressivePlankton.IsAlive == true)
                    {
                    ReducePlanktonSizeAggression(otherPlankton, component, aggressivePlankton);
                    }
                    if (!otherPlankton.IsAlive)
                    {
                        Log.Error($"{aggressivePlankton.SpeciesName}'s target is dead.");
                        continue;
                    }

                    // Check if the plankton instance should be removed (e.g., it was wiped out)
                    if (otherPlankton.CurrentSize <= 0)
                    {
                        planktonToRemove.Add(otherPlankton);
                    }
                }
            }
        }
    }



    // Remove the dead plankton species instances after the loop
    foreach (var plankton in planktonToRemove)
    {
        plankton.IsAlive = false;
    }
}


     private void PerformCarnivoreCheck(PlanktonComponent component)
{
    Log.Info($"Performing canivore check");
    // Collect all plankton instances that need to be deleted
    var planktonToRemoveCarnivore = new List<PlanktonComponent.PlanktonSpeciesInstance>();

    foreach (var planktonEntity in EntityManager.EntityQuery<PlanktonComponent>())
    {

        var carnivorousPlanktonInstances = component.SpeciesInstances
            .Where(inst => (inst.Diet == PlanktonComponent.PlanktonDiet.Carnivore))
            .ToList();


        if (carnivorousPlanktonInstances.Any())
        {
            foreach (var carnivorousPlankton in carnivorousPlanktonInstances)
            {
                 int carnivoreCount = carnivorousPlankton.CurrentSize;  // Total number of carnivores
                 float huntMultiplier = carnivoreCount * 0.05f;
                foreach (var otherPlankton in component.SpeciesInstances)
                {
                    if (carnivorousPlankton == otherPlankton) continue;
                    if (otherPlankton.IsAlive == true && carnivorousPlankton.IsAlive == true)
                    {
                        ReducePlanktonSizeCarnivorous(otherPlankton, component, carnivorousPlankton);
                    }

                    if (!otherPlankton.IsAlive)
                    {
                        Log.Error($"{carnivorousPlankton.SpeciesName} will start starving soon due to killing all prey.");
                    }

                    float sizeReduction = 0.5f + huntMultiplier;  // Multiply by the number of carnivores
                    ReducePlanktonSizeCarnivorous(otherPlankton,

                    // Check if the plankton instance should be removed
                    if (otherPlankton.CurrentSize <= 0)
                    {
                        planktonToRemoveCarnivore.Add(otherPlankton);
                    }
                }
            }
        }
    }



    // Remove the dead plankton species instances after the loop
    foreach (var plankton in planktonToRemoveCarnivore)
    {
        plankton.IsAlive = false;
    }
}



        private void ReducePlanktonSizeCarnivorous(PlanktonComponent.PlanktonSpeciesInstance planktonInstance, PlanktonComponent component, PlanktonComponent.PlanktonSpeciesInstance carnivorousPlankton, float sizeReduction)
        {
             planktonInstance.CurrentSize -= sizeReduction;
             Log.Info($"Reduced size of {planktonInstance.SpeciesName} to {planktonInstance.CurrentSize} via being predated on by {carnivorousPlankton.SpeciesName}");
             carnivorousPlankton.CurrentHunger += sizeReduction;
             Log.Info($"{carnivorousPlankton.SpeciesName} is now at {carnivorousPlankton.CurrentHunger} after hunting");

              if (planktonInstance.CurrentSize <= 0)
              {
                planktonInstance.CurrentSize = 0;
                planktonInstance.IsAlive = false;
                Log.Info($"{planktonInstance.SpeciesName} has been wiped out by {carnivorousPlankton.SpeciesName}.");
              }
        }

        private void ReducePlanktonSizeAggression(PlanktonComponent.PlanktonSpeciesInstance planktonInstance, PlanktonComponent component, PlanktonComponent.PlanktonSpeciesInstance aggressivePlankton)
        {
             float sizeReduction = 0.1f;
             planktonInstance.CurrentSize -= sizeReduction;
             component.DeadPlankton += sizeReduction;
             Log.Info($"Reduced size of {planktonInstance.SpeciesName} to {planktonInstance.CurrentSize} via being aggressively attacked by {aggressivePlankton.SpeciesName} ");

              if (planktonInstance.CurrentSize <= 0)
              {
                planktonInstance.CurrentSize = 0;
                planktonInstance.IsAlive = false;
                Log.Info($"{planktonInstance.SpeciesName} has been wiped out by {aggressivePlankton.SpeciesName}.");
                // change IsAlive once the framework is finished
              }
        }

         private void PlanktonHunger(PlanktonComponent component)
        {
            foreach (var planktonInstance in component.SpeciesInstances)
            {
            if (planktonInstance.IsAlive == true)
            {
                if (planktonInstance.CurrentHunger <= 0)
                {
                    // If hunger is 0 or less, plankton dies
                    if (planktonInstance.CurrentHunger <= 0f)
                    {
                        planktonInstance.CurrentHunger = 0f;
                        planktonInstance.IsAlive = false;
                        component.DeadPlankton += planktonInstance.CurrentSize;
                        Log.Error($"{planktonInstance.SpeciesName} has starved to death.");
                    }
                }
                else
                {
                    // Reduce hunger if greater than 0
                    var HungerLoss = 0.5f;
                    var HungerIncrease = 0.01f;
                    var hungerExponent = planktonInstance.CurrentSize * HungerIncrease + HungerLoss;

                    planktonInstance.CurrentHunger -= hungerExponent;
                    planktonInstance.CurrentHunger = Math.Max(0f, planktonInstance.CurrentHunger);  // Ensure it doesn't go below 0

                    Log.Error($"{planktonInstance.SpeciesName} has lost {HungerLoss} hunger. It is now at {planktonInstance.CurrentHunger}");
                }

            // Ensure hunger doesn't exceed 50 and log if full
            if (planktonInstance.CurrentHunger >= 51f)
            {
                planktonInstance.CurrentHunger = 50f;  // Clamp the hunger to a maximum of 50
                Log.Error($"{planktonInstance.SpeciesName} is full.");
            }
           }
           }
}

        private void PlanktonGrowth(PlanktonComponent component)
        {
            foreach (var planktonInstance in component.SpeciesInstances)
            {
                if (planktonInstance.IsAlive && planktonInstance.CurrentHunger >= 30 && planktonInstance.CurrentSize <= 100)
                {
                    var growthRate = 0.05f;
                    var growthExponent = growthRate * planktonInstance.CurrentSize;

                    planktonInstance.CurrentSize += growthExponent;
                    Log.Info($"{planktonInstance.SpeciesName} is a class-I plankton that has grown to size {planktonInstance.CurrentSize}");
                }
                if (planktonInstance.IsAlive && planktonInstance.CurrentHunger >= 45 && planktonInstance.CurrentSize <= 200)
                {
                    var growthRate = 0.02f;
                    var growthExponent = growthRate * planktonInstance.CurrentSize;

                    planktonInstance.CurrentSize += growthExponent;
                    Log.Info($"{planktonInstance.SpeciesName} is a class-II plankton that has grown to size {planktonInstance.CurrentSize}");
                }
                if (planktonInstance.IsAlive && planktonInstance.CurrentHunger >= 50 && planktonInstance.CurrentSize >= 200)
                {
                    var growthRate = 0.01f;
                    var growthExponent = growthRate * planktonInstance.CurrentSize;

                    planktonInstance.CurrentSize += growthExponent;
                    Log.Info($"{planktonInstance.SpeciesName} is a class-III plankton that has grown to size {planktonInstance.CurrentSize}");
                }
            }
        }

}
}

