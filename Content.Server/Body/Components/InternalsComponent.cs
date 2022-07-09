namespace Content.Server.Body.Components
{
    [RegisterComponent]
    public sealed class InternalsComponent : Component
    {
        [ViewVariables] public EntityUid? GasTankEntity { get; set; }
        [ViewVariables] public EntityUid? BreathToolEntity { get; set; }
    }
}
