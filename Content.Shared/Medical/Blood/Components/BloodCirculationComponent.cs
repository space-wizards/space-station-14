using Content.Shared.FixedPoint;
using Content.Shared.Medical.Blood.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Blood.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodCirculationComponent : Component
{

    #region Simulation

    /// <summary>
    /// Vascular resistance, aka how much your blood vessels resist the flow of blood
    /// Used to calculate blood pressure, expressed as a percentage from 0-100
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public FixedPoint2 VascularResistance = 100;

    /// <summary>
    /// Cached value for cardiac output
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public FixedPoint2 CardiacOutput ;

    /// <summary>
    /// The healthy volume for this bloodstream.
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: Required
    public FixedPoint2 HealthyVolume = 250;

    /// <summary>
    /// The healthy high (diastolic) pressure.
    /// this is the blood pressure at the moment blood is pumped
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 HealthyHighPressure = 120;

    /// <summary>
    /// The healthy high (diastolic) pressure.
    /// this is the blood pressure in between blood pumps
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 HealthyLowPressure = 80;

    /// <summary>
    /// Cached raw blood pressure value
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly),AutoNetworkedField]
    public FixedPoint2 RawPressure = 0;

    /// <summary>
    /// Constant used to calculate blood diastolic blood pressure
    /// This is a cached value calculated from HealthyHighPressure
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public FixedPoint2 LowPressureMod;

    /// <summary>
    /// Constant used to calculate blood diastolic blood pressure
    /// This is a cached value calculated from HealthyLowPressure
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public FixedPoint2 HighPressureMod;

    #endregion

    #region BloodGroups

    /// <summary>
    /// This prototype defines the blood that is used in this bloodstream, specifically which reagents are used and
    /// the linked defined bloodtypes. It also provides the list of possible bloodtypes and the likelihood of
    /// spawning with a particular one.
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: Required
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
    /// Which antigens are allowed in this bloodstream. If an antigen is not on this list
    /// it will cause a blood toxicity response (and that is very bad). This value is populated
    /// when a bloodtype is selected. Note: this functionality is disabled when EnforceBloodTypes is false!
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<BloodAntigenPrototype>> AllowedAntigens = new();

    #endregion
}
