namespace Content.Shared.Chemistry.Components.Solutions;

[RegisterComponent]
public sealed partial class SolutionHeatDirtyComponent : Component
{
    //TODO: CVAR this, make it 'SolutionUpdateInterval' and use it for rate reactions too
    public static readonly TimeSpan HeatCapacityUpdateInterval = TimeSpan.FromSeconds(1f);
}
