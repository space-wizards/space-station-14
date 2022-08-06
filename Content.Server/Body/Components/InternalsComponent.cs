namespace Content.Server.Body.Components
{
    /// <summary>
    /// Handles hooking up a mask (breathing tool) / gas tank together and allowing the Owner to breathe through it.
    /// </summary>
    [RegisterComponent]
    public sealed class InternalsComponent : Component
    {
        [ViewVariables] public EntityUid? GasTankEntity { get; set; }
        [ViewVariables] public EntityUid? BreathToolEntity { get; set; }
    }
}
