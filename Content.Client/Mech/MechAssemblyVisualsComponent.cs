namespace Content.Client.Mech;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class MechAssemblyVisualsComponent : Component
{
    [DataField("statePrefix", required: true)]
    public string StatePrefix = string.Empty;
}
