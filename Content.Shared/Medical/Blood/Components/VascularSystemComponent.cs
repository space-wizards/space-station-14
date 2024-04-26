using Content.Shared.FixedPoint;
using Content.Shared.Medical.Blood.Prototypes;
using Content.Shared.Medical.Blood.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Blood.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VascularSystemComponent : Component
{

    #region Simulation

    /// <summary>
    /// The healthy volume for this bloodstream.
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: Required
    public FixedPoint2 HealthyVolume = 250;

    /// <summary>
    /// Healthy blood pressure values.
    /// The first value is: high (systolic) pressure. This is the blood pressure at the moment blood is pumped.
    /// The second value is: low (diastolic) pressure. This is the blood pressure in between pumps.
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: Required
    public BloodPressure HealthyBloodPressure = (120,0);

    /// <summary>
    /// The current blood pressure. With the first value being high and the second being low
    /// If this value is null at mapinit, it will be populated.
    /// This value will become null if there are no circulationEntities or if there is no cardiac output (aka heart is fucked)
    /// </summary>
    [DataField, AutoNetworkedField]
    public BloodPressure? CurrentBloodPressure = null;

    /// <summary>
    /// The current cached pulse rate, this is the fastest rate from any of the circulation entities
    /// This value will become null if there are no circulationEntities or if there is no cardiac output (aka heart is fucked)
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2? Pulse = null;

    /// <summary>
    /// Vascular resistance, aka how much your blood vessels resist the flow of blood
    /// Used as a multiplier for VascularConstant to calculate blood pressure, expressed as a percentage from 0-1
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public FixedPoint2 VascularResistance = 1;

    /// <summary>
    /// Entities that are pumping this blood
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<EntityUid> CirculationEntities ;

    /// <summary>
    /// Cached optimal CardiacOutput value, combined value from all the circulation entities
    /// This is mainly used for re-calculating vascular constants
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public FixedPoint2 OptimalCardiacOutput;

    /// <summary>
    /// Constant used to calculate blood diastolic blood pressure
    /// This is calculated based on the healthy low pressure and
    /// multiplied by VascularResistance before being used
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float LowPressureVascularConstant;

    /// <summary>
    /// Constant used to calculate blood diastolic blood pressure
    /// This is calculated based on the healthy high pressure and
    /// multiplied by VascularResistance before being used
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float HighPressureVascularConstant;

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
