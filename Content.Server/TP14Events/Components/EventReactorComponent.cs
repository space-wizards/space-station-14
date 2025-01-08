namespace Content.Server.Event.Components;

/// <summary>
/// Flickers all the lights within a certain radius.
/// </summary>
[RegisterComponent]
public sealed partial class EventReactorComponent : Component
{
    /// <summary>
    /// Lights within this radius will be flickered on activation
    /// </summary>
    [DataField("radius")]
    public float Radius = 9999;

    /// <summary>
    /// The chance that the light will flicker
    /// </summary>
    [DataField("flickerChance")]
    public float FlickerChance = 1f;
}
