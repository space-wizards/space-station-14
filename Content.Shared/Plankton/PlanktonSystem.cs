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
                Log.Error("The plankton fucking died.");
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
            Log.Info($"Plankton Initialized: Diet: {component.Diet}, Characteristics: {component.Characteristics}, Living inside: {component.ReagentId}");

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
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Bioluminescent) != 0)
            {
                Log.Error("Plankton is bioluminescent");
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Mimicry) != 0)
            {
                Log.Error("Plankton is a mimic");
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.ChemicalProduction) != 0)
            {
                Log.Error("Plankton produces chemicals");
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.MagneticField) != 0)
            {
                Log.Error("Plankton produces a magnetic field");
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Hallucinogenic) != 0)
            {
                Log.Error("Plankton makes you high");
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.PheromoneGlands) != 0)
            {
                Log.Error("Plankton produces pheromones");
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.PolypColony) != 0)
            {
                Log.Error("Plankton produces coral");
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.AerosolSpores) != 0)
            {
                Log.Error("Plankton produces spores");
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.HyperExoticSpecies) != 0)
            {
                Log.Error("Plankton is hyper-exotic");
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Sentience) != 0)
            {
                Log.Error("Plankton is sentient");
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Pyrophilic) != 0)
            {
                Log.Error("Plankton is happiest in heat");
            }

            if ((component.Characteristics & PlanktonComponent.PlanktonCharacteristics.Cryophilic) != 0)
            {
                Log.Error("Plankton is happiest in cold");
            }
        }
    }
}
