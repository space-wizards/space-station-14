using Content.Server.Planktonics;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;

namespace Content.Shared.Planktonics
{
    public sealed class PlanktonGenerationSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlanktonComponent, ComponentInit>(OnPlanktonCompInit);
        }

        private void OnPlanktonCompInit(EntityUid uid, PlanktonComponent component, ComponentInit args)
        {
            var random = new Random();
            var reagentId = component.ReagentId.ToString();

            component.ReagentId = reagentId;

            // Check if the reagent is SeaWater
            if (reagentId != "SeaWater")
            {
                component.IsAlive = false;
                Log.Error("The {component.Name} fucking died.");
            }

            // Randomly generate plankton name
            var firstName = (PlanktonComponent.PlanktonFirstName)random.Next(Enum.GetValues<PlanktonComponent.PlanktonFirstName>().Length);
            var secondName = (PlanktonComponent.PlanktonSecondName)random.Next(Enum.GetValues<PlanktonComponent.PlanktonSecondName>().Length);
            component.Name = new PlanktonComponent.PlanktonName(firstName.ToString(), secondName.ToString());
            
            // Log the generated plankton name
            Log.Info($"Plankton species: {component.Name}");

            // Generate 2-3 random characteristics
            int numCharacteristics = random.Next(2, 4);  // Randomly pick 2-3 characteristics
            var possibleCharacteristics = Enum.GetValues(typeof(PlanktonComponent.PlanktonCharacteristics)); // Get enum values
            var selectedCharacteristics = new HashSet<PlanktonComponent.PlanktonCharacteristics>();

            while (selectedCharacteristics.Count < numCharacteristics)
            {
                var randomCharacteristic = (PlanktonComponent.PlanktonCharacteristics)possibleCharacteristics.GetValue(random.Next(possibleCharacteristics.Length));
                selectedCharacteristics.Add(randomCharacteristic);
            }

            // Assign the selected characteristics to the plankton component
            component.Characteristics = 0; // Initialize the characteristics to 0
            foreach (var characteristic in selectedCharacteristics)
            {
                component.Characteristics |= characteristic;
            }

            // Log the plankton characteristics
            Log.Info($"Plankton Initialized: Name: {component.Name}, Diet: {component.Diet}, Characteristics: {component.Characteristics}, Living inside: {component.ReagentId}");

            // Handle plankton interactions
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
