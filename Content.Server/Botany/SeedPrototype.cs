using Content.Server.Botany.Components;
using Content.Shared.Database;
using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Botany;

[Prototype]
public sealed partial class SeedPrototype : SeedData, IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
}

[DataDefinition]
public partial struct SeedChemQuantity
{
    /// <summary>
    /// Minimum amount of chemical that is added to produce, regardless of the potency
    /// </summary>
    [DataField]
    public int Min;

    /// <summary>
    /// Maximum amount of chemical that can be produced after taking plant potency into account.
    /// </summary>
    [DataField]
    public int Max;

    /// <summary>
    /// When chemicals are added to produce, the potency of the seed is divided with this value. Final chemical amount is the result plus the `Min` value.
    /// Example: PotencyDivisor of 20 with seed potency of 55 results in 2.75, 55/20 = 2.75. If minimum is 1 then final result will be 3.75 of that chemical, 55/20+1 = 3.75.
    /// </summary>
    [DataField]
    public int PotencyDivisor;

    /// <summary>
    /// Inherent chemical is one that is NOT result of mutation or crossbreeding. These chemicals are removed if species mutation is executed.
    /// </summary>
    [DataField]
    public bool Inherent = true;
}

[Virtual, DataDefinition]
// TODO Make Botany ECS and give it a proper API. I removed the limited access of this class because it's egregious how many systems needed access to it due to a lack of an actual API.
/// <remarks>
/// SeedData is no longer restricted because the number of friends is absolutely unreasonable.
/// This entire data definition is unreasonable. I felt genuine fear looking at this, this is horrific. Send help.
/// </remarks>
// TODO: Hit Botany with hammers
public partial class SeedData
{
    #region Tracking

    /// <summary>
    /// The name of this seed. Determines the name of seed packets.
    /// </summary>
    [DataField]
    public string Name { get; private set; } = "";

    /// <summary>
    /// The noun for this type of seeds. E.g. for fungi this should probably be "spores" instead of "seeds". Also
    /// used to determine the name of seed packets.
    /// </summary>
    [DataField]
    public string Noun { get; private set; } = "";

    /// <summary>
    /// Name displayed when examining the hydroponics tray. Describes the actual plant, not the seed itself.
    /// </summary>
    [DataField]
    public string DisplayName { get; private set; } = "";

    [DataField] public bool Mysterious;

    /// <summary>
    /// If true, the properties of this seed cannot be modified.
    /// </summary>
    [DataField]
    public bool Immutable;

    /// <summary>
    /// If true, there is only a single reference to this seed and it's properties can be directly modified without
    /// needing to clone the seed.
    /// </summary>
    [ViewVariables]
    public bool Unique = false; // seed-prototypes or yaml-defined seeds for entity prototypes will not generally be unique.
    #endregion

    #region Output
    /// <summary>
    /// The entity prototype that is spawned when this type of seed is extracted from produce using a seed extractor.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PacketPrototype = "SeedBase";

    /// <summary>
    /// The entity prototypes that are spawned when this type of seed is harvested.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> ProductPrototypes = [];

    [DataField]
    public Dictionary<string, SeedChemQuantity> Chemicals = [];

    #endregion

    #region General traits



    #endregion

    #region Cosmetics

    [DataField(required: true)]
    public ResPath PlantRsi { get; set; } = default!;

    [DataField]
    public string PlantIconState { get; set; } = "produce";

    /// <summary>
    /// Screams random sound from collection SoundCollectionSpecifier
    /// </summary>
    [DataField]
    public SoundSpecifier ScreamSound = new SoundCollectionSpecifier("PlantScreams", AudioParams.Default.WithVolume(-10));

    /// <summary>
    /// Which kind of kudzu this plant will turn into if it kuzuifies.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string KudzuPrototype = "WeakKudzu";

    #endregion

    /// <summary>
    /// The mutation effects that have been applied to this plant.
    /// </summary>
    [DataField]
    public List<RandomPlantMutation> Mutations { get; set; } = [];

    /// <summary>
    /// The seed prototypes this seed may mutate into when prompted to.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<SeedPrototype>))]
    public List<string> MutationPrototypes = [];

    /// <summary>
    /// The growth components used by this seed.
    /// TODO: Delete after plants transition to entities
    /// </summary>
    [DataField]
    public GrowthComponentsHolder GrowthComponents = new();

    /// <summary>
    /// Log impact for harvest operations.
    /// </summary>
    [DataField]
    public LogImpact? HarvestLogImpact;

    /// <summary>
    /// Log impact for plant operations.
    /// </summary>
    [DataField]
    public LogImpact? PlantLogImpact;

    public SeedData Clone()
    {
        DebugTools.Assert(!Immutable, "There should be no need to clone an immutable seed.");
        var serializationManager = IoCManager.Resolve<ISerializationManager>();

        var newSeed = new SeedData
        {
            GrowthComponents = serializationManager.CreateCopy(GrowthComponents, notNullableOverride: true),
            HarvestLogImpact = HarvestLogImpact,
            PlantLogImpact = PlantLogImpact,
            Name = Name,
            Noun = Noun,
            DisplayName = DisplayName,
            Mysterious = Mysterious,

            PacketPrototype = PacketPrototype,
            ProductPrototypes = new List<string>(ProductPrototypes),
            MutationPrototypes = new List<string>(MutationPrototypes),
            Chemicals = new Dictionary<string, SeedChemQuantity>(Chemicals),

            PlantRsi = PlantRsi,
            PlantIconState = PlantIconState,
            Mutations = new List<RandomPlantMutation>(Mutations),

            // Newly cloned seed is unique. No need to unnecessarily clone if repeatedly modified.
            Unique = true,
        };

        newSeed.Mutations.AddRange(Mutations);
        return newSeed;
    }


    /// <summary>
    /// Handles copying most species defining data from 'other' to this seed while keeping the accumulated mutations intact.
    /// </summary>
    public SeedData SpeciesChange(SeedData other)
    {
        var serializationManager = IoCManager.Resolve<ISerializationManager>();
        var newSeed = new SeedData
        {
            GrowthComponents = serializationManager.CreateCopy(other.GrowthComponents, notNullableOverride: true),
            HarvestLogImpact = other.HarvestLogImpact,
            PlantLogImpact = other.PlantLogImpact,
            Name = other.Name,
            Noun = other.Noun,
            DisplayName = other.DisplayName,
            Mysterious = other.Mysterious,

            PacketPrototype = other.PacketPrototype,
            ProductPrototypes = new List<string>(other.ProductPrototypes),
            MutationPrototypes = new List<string>(other.MutationPrototypes),

            Chemicals = new Dictionary<string, SeedChemQuantity>(Chemicals),

            Mutations = Mutations,

            PlantRsi = other.PlantRsi,
            PlantIconState = other.PlantIconState,

            // Newly cloned seed is unique. No need to unnecessarily clone if repeatedly modified.
            Unique = true,
        };

        // Adding the new chemicals from the new species.
        foreach (var otherChem in other.Chemicals)
        {
            newSeed.Chemicals.TryAdd(otherChem.Key, otherChem.Value);
        }

        // Removing the inherent chemicals from the old species. Leaving mutated/crossbread ones intact.
        foreach (var originalChem in newSeed.Chemicals)
        {
            if (!other.Chemicals.ContainsKey(originalChem.Key) && originalChem.Value.Inherent)
            {
                newSeed.Chemicals.Remove(originalChem.Key);
            }
        }

        return newSeed;
    }
}
