using Content.Server.Planktonics;
using Robust.Shared.Random;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.Planktonics
{
    public sealed class PlanktonGenerationSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlanktonComponent, ComponentInit>(OnPlanktonCompInit);
        }

        private void OnPlanktonCompInit(EntityUid uid, PlanktonComponent component, ComponentInit args)
        {
                    var random = new Random();
                    var reagentId = Solution.GetPrimaryReagentId();

                    component.ReagentId = reagentId;

                    if (_prototypeManager.TryIndex<ReagentPrototype>(prototypeId, out var reagentPrototype))
                    {
                        if (!reagentId == "SeaWater")
                        {
                            Log.Error("The plankton fucking died.");
                        }
                    }
                    Log.Error($"Plankton is in reagent: {reagentId.Prototype}");
                    
    // Generate 2-3 random characteristics by choosing a small number of random flags
    int numCharacteristics = random.Next(2, 4);
    var possibleCharacteristics = Enum.GetValues<PlanktonComponent.PlanktonCharacteristics>();
    var selectedCharacteristics = new HashSet<PlanktonComponent.PlanktonCharacteristics>();

    while (selectedCharacteristics.Count < numCharacteristics)
    {
        var randomCharacteristic = (PlanktonComponent.PlanktonCharacteristics)possibleCharacteristics.GetValue(random.Next(possibleCharacteristics.Length));
        selectedCharacteristics.Add(randomCharacteristic);
    }

            Log.Error($"Plankton Initialized: Diet: {component.Diet}, Characteristics: {component.Characteristics}, Living inside: {component.ReagentId}");

            PlanktonInteraction(uid);
        }

        private void PlanktonInteraction(EntityUid uid)
        {
            if (!_entityManager.TryGetComponent(uid, out PlanktonComponent component))
            {
                Log.Error($"No PlanktonComponent found for entity {uid}");
                return;
            }

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
