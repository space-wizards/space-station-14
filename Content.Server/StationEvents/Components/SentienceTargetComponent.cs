namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed class SentienceTargetComponent : Component
{
    private string _flavorKind = string.Empty;
    
    [DataField("flavorKind", required: true)]
    public string FlavorKind
    {
        get => _flavorKind;
        private set => _flavorKind = Loc.GetString(value);
    }
}
