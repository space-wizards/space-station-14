using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Circulatory.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodstreamComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdate = new TimeSpan();

    #region SimulationRelated

    /// <summary>
    /// The healthy volume for this bloodstream.
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: Required
    public FixedPoint2 HealthyVolume = 250;

    /// <summary>
    /// The maximum volume for this bloodstream (this may be exceeded with heavy negative effects)
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: Required
    public FixedPoint2 MaxVolume = 300;

    /// <summary>
    /// How much blood is currently being lost by bleed, this number must be positive
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Bloodloss = 0;

    #endregion

    #region SolutionRelated

    public const string BloodSolutionId = "bloodstream";

    public const string SpillSolutionId = "bloodSpill";

    /// <summary>
    /// The bloodstream solution
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? BloodSolution = null;

    /// <summary>
    /// The solution to "drip" onto the ground or soak into clothing
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SpillSolution  = null;

    [DataField]
    public FixedPoint2 BleedPuddleThreshold = 1.0f;

    /// <summary>
    /// The reagent that represents the combination of both bloodcells and plasma.
    /// This is the reagent used as blood in bloodstream.
    /// This is a cached value from BloodDefinitionPrototype and should not be modified!
    /// </summary>
    [DataField, AutoNetworkedField]
    public string BloodReagent = string.Empty;

    #endregion

    #region BloodGroups

    /// <summary>
    /// This prototype defines the blood that is used in this bloodstream, specifically which reagents are used and
    /// the linked defined bloodtypes. It also provides the list of possible bloodtypes and the likelihood of
    /// spawning with a particular one.
    /// </summary>
    [DataField(), AutoNetworkedField] //TODO: Required
    public string BloodDefinition = string.Empty; //TODO: convert back to protoID

    /// <summary>
    /// If this is defined, it will set this bloodstream to use the specified bloodtype.
    /// Make sure the bloodtype is in the listed types in bloodDefinition if EnforceBloodTypes is true or
    /// immediate blood poisoning will occur (maybe you want that I'm not judging).
    /// If this is null, the bloodtype will be randomly selected from the bloodtypes defined in the blood definition
    /// according to their probabilities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? BloodType = null; //TODO: convert back to protoID

    /// <summary>
    /// If set to true, check blood transfusions to make sure that they do not contain antibodies that are
    /// not on the allowed list. If set to false, skip antibody and blood toxicity checks
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool EnforceBloodTypes = true;

    /// <summary>
    /// Which antibodies are allowed in this bloodstream. If an antibody is not on this list
    /// it will cause a blood toxicity response (and that is very bad). This value is populated
    /// when a bloodtype is selected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> AllowedAntibodies = new();

    #endregion




}
