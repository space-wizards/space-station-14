namespace Content.Client.Mech;

/// <summary>
/// This is used for visualizing mech constructions
/// </summary>
[RegisterComponent]
public sealed class MechAssemblyVisualsComponent : Component
{
    /// <summary>
    /// The prefix that is followed by the number which
    /// denotes the current state to use.
    /// </summary>
    [DataField("statePrefix", required: true)]
    public string StatePrefix = string.Empty;
}
