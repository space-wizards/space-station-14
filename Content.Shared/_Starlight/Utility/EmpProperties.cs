using Robust.Shared.Utility;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Starlight.Utility;

[Serializable, NetSerializable]
[DataDefinition]
public partial class EmpProperties
{
    
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 1.0f;
    
    /// <summary>
    /// How much energy will be consumed per battery in range
    /// </summary>
    [DataField("energyConsumption"), ViewVariables(VVAccess.ReadWrite)]
    public float EnergyConsumption;

    /// <summary>
    /// How long it disables targets in seconds
    /// </summary>
    [DataField("disableDuration"), ViewVariables(VVAccess.ReadWrite)]
    public float DisableDuration = 60f;

    public EmpProperties(float range, float energyConsumption, float disableDuration)
    {
        Range = range;
        EnergyConsumption = energyConsumption;
        DisableDuration = disableDuration;
    }
}