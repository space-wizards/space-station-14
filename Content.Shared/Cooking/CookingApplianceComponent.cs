namespace Content.Shared.Cooking;

[RegisterComponent]
public sealed partial class CookingApplianceComponent : Component
{
    [DataField] 
    public string ApplianceType = ""; // should be a type thingi not a string
}
    
    
