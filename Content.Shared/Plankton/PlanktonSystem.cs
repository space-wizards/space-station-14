using Content.Server.Planktonics;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;

namespace Content.Shared.Planktonics
{
  private void OnPlanktonCompInit(EntityUid uid, PlanktonComponent component, ComponentInit args)
{
    var random = new Random();

    // Ensure we don't mess with existing logic for getting the reagent
    var reagentId = solution.GetPrimaryReagentId(); // Keep this intact, as it's essential.
    component.ReagentId = reagentId;

    // If not in "SeaWater", mark as dead and exit
    if (reagentId != "SeaWater")
    {
        component.IsAlive = false;
        Log.Error("The plankton component died due to an invalid environment.");
        return; // Exit early if the plankton isn't in the right environment
    }

    // Generate 3 plankton species with random characteristics and names
    for (int i = 0; i < 3; i++)
    {
        // Randomly generate plankton name from static name lists
        var firstName = PlanktonComponent.PlanktonFirstNames[random.Next(PlanktonComponent.PlanktonFirstNames.Length)];
        var secondName = PlanktonComponent.PlanktonSecondNames[random.Next(PlanktonComponent.PlanktonSecondNames.Length)];
        var planktonName = new PlanktonComponent.PlanktonName(firstName, secondName);

        // Randomly generate 2-3 characteristics per plankton
        int numCharacteristics = random.Next(2, 4);  // Randomly pick 2-3 characteristics
        var possibleCharacteristics = Enum.GetValues(typeof(PlanktonComponent.PlanktonCharacteristics));
        var selectedCharacteristics = new HashSet<PlanktonComponent.PlanktonCharacteristics>();

        while (selectedCharacteristics.Count < numCharacteristics)
        {
            var randomCharacteristic = (PlanktonComponent.PlanktonCharacteristics)possibleCharacteristics.GetValue(random.Next(possibleCharacteristics.Length));
            selectedCharacteristics.Add(randomCharacteristic);
        }

        // Combine characteristics using bitwise OR
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
            1.0f // For now, just assign a default size
        );

        // Add the plankton species instance to the SpeciesInstances list
        component.SpeciesInstances.Add(planktonInstance);

        // Log the generated plankton species details
        Log.Info($"Generated plankton species {planktonInstance.SpeciesName} with characteristics {combinedCharacteristics}");
    }

    // Log the total number of plankton species initialized
    Log.Info($"Plankton component initialized with {component.SpeciesInstances.Count} species.");

    // Check plankton interactions
    PlanktonInteraction(uid);
}



        private void PlanktonInteraction(EntityUid uid)
        {
            if (!_entityManager.TryGetComponent(uid, out PlanktonComponent component))
            {
                Log.Error($"No PlanktonComponent found for entity {uid}");
                return;
            }

            // Check for specific characteristics
            CheckPlanktonCharacteristics(component);
        }

        private void CheckPlanktonCharacteristics(PlanktonComponent component)
        {
            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Aggressive) != 0)
            {
                Log.Error("Plankton is aggressive");
                // Handle aggressive plankton interactions, e.g., attack nearby entities
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Bioluminescent) != 0)
            {
                Log.Info("Plankton is bioluminescent");
                // Handle bioluminescent plankton behavior, e.g., light up certain areas
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Mimicry) != 0)
            {
                Log.Info("Plankton is a mimic");
                // Handle mimicry, plankton might disguise itself as something else
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.ChemicalProduction) != 0)
            {
                Log.Info("Plankton produces chemicals");
                // Handle chemical production, plankton might affect nearby entities with toxins or nutrients
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.MagneticField) != 0)
            {
                Log.Info("Plankton produces a magnetic field");
                // Handle magnetic field generation, affecting nearby electronic devices or entities
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Hallucinogenic) != 0)
            {
                Log.Info("Plankton makes you high");
                // Handle hallucinogenic effects, plankton could cause status effects like confusion or hallucinations
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.PheromoneGlands) != 0)
            {
                Log.Info("Plankton produces pheromones");
                // Handle pheromone production, plankton might attract or repel other organisms
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.PolypColony) != 0)
            {
                Log.Info("Plankton forms a polyp colony");
                // Handle colony formation, plankton could form a larger entity or structure over time
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.AerosolSpores) != 0)
            {
                Log.Info("Plankton produces spores");
                // Handle aerosol spores, plankton might spread its spores over a wide area
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.HyperExoticSpecies) != 0)
            {
                Log.Info("Plankton is hyper-exotic");
                // Handle hyper-exotic species behavior, plankton could be highly specialized or rare
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Sentience) != 0)
            {
                Log.Info("Plankton is sentient");
                // Handle sentience, plankton might have the ability to think or communicate
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Pyrophilic) != 0)
            {
                Log.Info("Plankton is happiest in heat");
                // Handle pyrophilic behavior, plankton might thrive in hot environments
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Cryophilic) != 0)
            {
                Log.Info("Plankton is happiest in cold");
                // Handle cryophilic behavior, plankton might thrive in cold environments
            }

            // You can expand this to check for more conditions or create new interactions based on other characteristics
        }
    }
}
