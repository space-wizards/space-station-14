using Content.Shared.Planktonics;

namespace Content.Server.Planktonics;

[RegisterComponent]
public sealed partial class PlanktonComponent : Component
{
    [DataField("isAlive"), ViewVariables(VVAccess.ReadWrite)]
    public bool IsAlive = true; // Is the plankton alive?

    [DataField("reagentId")]
    public ReagentId ReagentId { get; set; } = new ReagentId();

    // This holds all plankton species instances for this reagent
    public List<PlanktonSpeciesInstance> SpeciesInstances { get; set; } = new(); 

    // Define a class that represents each plankton species
    public class PlanktonSpeciesInstance
    {
        public string SpeciesName { get; set; }
        public PlanktonDiet Diet { get; set; }
        public PlanktonCharacteristics Characteristics { get; set; }
        public float CurrentSize { get; set; }

        public PlanktonSpeciesInstance(string speciesName, PlanktonDiet diet, PlanktonCharacteristics characteristics, float currentSize)
        {
            SpeciesName = speciesName;
            Diet = diet;
            Characteristics = characteristics;
            CurrentSize = currentSize;
        }
    }

    // Define a Diet Type Enum
    public enum PlanktonDiet
    {
        Carnivore,
        Photosynthetic,
        Radiophage,
        Saguinophage,
        Electrophage,
        Symbiotroph,
        Chemophage,
        Decomposer,
        Cryophilic,
        Pyrophilic,
        Scavenger
    }

    // Define Characteristics Enum (bitwise flags)
    [Flags]
    public enum PlanktonCharacteristics
    {
        Aggressive = 1 << 0,
        Bioluminescent = 1 << 1,
        Parasitic = 1 << 2,
        Radioactive = 1 << 3,
        Charged = 1 << 4,
        Pyrotechnic = 1 << 5,
        Mimicry = 1 << 6,
        ChemicalProduction = 1 << 7,
        MagneticField = 1 << 8,
        Hallucinogenic = 1 << 9,
        PheromoneGlands = 1 << 10,
        PolypColony = 1 << 11,
        AerosolSpores = 1 << 12,
        HyperExoticSpecies = 1 << 13,
        Sentience = 1 << 14,
        Pyrophilic = 1 << 15,
        Cryophilic = 1 << 16
    }

    // These hold the random generated values for the plankton
    [DataField("diet"), ViewVariables(VVAccess.ReadWrite)]
    public PlanktonDiet Diet { get; set; }

    [DataField("characteristics"), ViewVariables(VVAccess.ReadWrite)]
    public PlanktonCharacteristics Characteristics { get; set; }

    // Additional properties
    [DataField("temperatureToleranceLow"), ViewVariables(VVAccess.ReadWrite)]
    public float TemperatureToleranceLow { get; set; } = 0.0f; // Min temperature tolerance

    [DataField("temperatureToleranceHigh"), ViewVariables(VVAccess.ReadWrite)]
    public float TemperatureToleranceHigh { get; set; } = 40.0f; // Max temperature tolerance
}
