using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Cooking;

[RegisterComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CookingComponent : Component
{
    /// <summary>
    /// What appliance this needs for cooking
    /// </summary>
    [DataField] 
    public string Appliance = ""; // this should be a type thingi not a string

    /// <summary>
    /// Penalty 
    /// </summary>
    [DataField] 
    public double WrongAppliancePenalty = 0.5;
    
    /// <summary>
    /// Minimum temperature the entity must reach before it starts cooking
    /// </summary>
    [DataField] 
    public double MinimumTemperature;
    
    /// <summary>
    /// Time the entity has been cooking for
    /// </summary>
    [DataField]
    public double CookingTime;

    /// <summary>
    /// How fast the cooking time advances when the entity is cooking
    /// </summary>
    [DataField] 
    public double CookingSpeed = 1;
    
    /// <summary>
    /// Is the entity currently cooking?
    /// </summary>
    [DataField]
    public bool IsCooking;
    
    /// <summary>
    /// How long the entity must be cooking for to advance onto the cooked state
    /// </summary>
    [DataField]
    public double TimeCookedThreshold;

    /// <summary>
    /// How long the entity must be cooking for to advance onto the burnt state
    /// </summary>
    [DataField] 
    public float TimeBurntThreshold;
    
    /// <summary>
    /// The next time this component will be updated
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The time between updates checking if the entity should be cooking
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
}

