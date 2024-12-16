using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Plankton;

[RegisterComponent]
public partial class PlanktonComponent : Component
{
    [DataField("isAlive"), ViewVariables(VVAccess.ReadWrite)]
    public bool IsAlive = true; // Is the plankton alive?

    [DataField("reagentId")]
    public ReagentId ReagentId { get; set; } = new ReagentId();

     [DataField("deadPlankton")]
    public DeadPlankton DeadPlankton { get; set; } = new DeadPlankton();

    // This holds all plankton species instances for this reagent
    public List<PlanktonSpeciesInstance> SpeciesInstances { get; set; } = new();

    // Define a class that represents each plankton species
    public class PlanktonSpeciesInstance
    {
        public PlanktonName SpeciesName { get; set; }
        public PlanktonDiet Diet { get; set; }
        public PlanktonCharacteristics Characteristics { get; set; }
        public float CurrentSize { get; set; }

        public PlanktonSpeciesInstance(PlanktonName speciesName, PlanktonDiet diet, PlanktonCharacteristics characteristics, float currentSize)
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

    // Define large lists of "scientific" first and second names for plankton
    public static readonly string[] PlanktonFirstNames =
    {
        "Acanthocystis", "Actinophrys", "Amphora", "Apistosporus", "Aulacodiscus",
        "Brachionus", "Cladocera", "Coscinodiscus", "Didinium", "Diatoma",
        "Entomorpha", "Euglena", "Gloeocapsa", "Leptocylindrus", "Mastigophora",
        "Mesorhizobium", "Navicula", "Nitzschia", "Oscillatoria", "Phaeodactylum",
        "Phacus", "Platymonas", "Protoperidinium", "Pyramimonas", "Spirulina",
        "Synedra", "Tetradontia", "Trachelomonas", "Volvox", "Vorticella"
    };

    public static readonly string[] PlanktonSecondNames =
    {
        "longispina", "latifolia", "quadricaudata", "gracilis", "bioluminescens",
        "radiata", "toxica", "cystiformis", "fimbriata", "planctonica",
        "viridis", "globosa", "aurelia", "pulchra", "reducta",
        "tuberculata", "subtilis", "hyalina", "cephalopodiformis", "corymbosa",
        "parasitica", "electrica", "xenofila", "macrospora", "fluorescens",
        "lucida", "cyanobacteria", "multicellularis", "carotenoides", "ectoplasmica"
    };

    // Class to combine the first and second name for plankton species
    public class PlanktonName
    {
        public string FirstName { get; set; }
        public string SecondName { get; set; }

        public PlanktonName(string firstName, string secondName)
        {
            FirstName = firstName;
            SecondName = secondName;
        }

        // Overriding ToString to provide a formatted name
        public override string ToString()
        {
            return $"{FirstName} {SecondName}";
        }
    }
}
