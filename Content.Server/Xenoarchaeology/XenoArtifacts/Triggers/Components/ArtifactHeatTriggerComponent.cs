namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

// TODO: This should probably be generalized for cold temperature too,
// but right now there is no sane way to make a freezer.

/// <summary>
///     Triggers artifact if its in hot environment or
///     has contacted with a hot object (lit welder, lighter, etc).
/// </summary>
[RegisterComponent]
public sealed class ArtifactHeatTriggerComponent : Component
{
    /// <summary>
    ///     Surrounding gas temperature to trigger artifact.
    ///     Around 300* celsius by default.
    ///     Doesn't affect hot items temperature.
    /// </summary>
    [DataField("activationTemperature")]
    public float ActivationTemperature = 573;

    /// <summary>
    ///     Should artifact be activated by hot items (welders, lighter, etc)?
    /// </summary>
    [DataField("activateHot")]
    public bool ActivateHotItems = true;
}
