using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Circulatory.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodstreamComponent : Component
{
    [DataField]
    public TimeSpan NextUpdate;

    #region SimulationRelated

    /// <summary>
    /// The healthy volume for this bloodstream.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 HealthyVolume = 0;

    /// <summary>
    /// The maximum volume for this bloodstream (this may be exceeded with heavy negative effects)
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxVolume = 0;

    /// <summary>
    /// This is the value that blood gets changed by each update (aka Delta).
    /// This value can be positive or negative, for example: a positive value would be if the entity is receiving a transfusion
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodDelta = 0;




    #endregion

    #region SolutionRelated

    /// <summary>
    /// The bloodstream solution
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? BloodSolution;

    /// <summary>
    /// The solution to "drip" onto the ground or soak into clothing
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SpillSolution;

    /// <summary>
    /// The reagent that represents the combination of both bloodcells and plasma.
    /// This is the reagent used as blood in bloodstream.
    /// This is a cached value from BloodDefinitionPrototype and should not be modified!
    /// </summary>
    [DataField, AutoNetworkedField]
    public string BloodReagent;

    #endregion

    #region BloodGroups

    /// <summary>
    /// This prototype defines the blood that is used in this bloodstream, specifically which reagents are used and
    /// the linked defined bloodtypes. It also provides the list of possible bloodtypes and the likelihood of
    /// spawning with a particular one.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string BloodGroup; //TODO: convert back to protoID

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
