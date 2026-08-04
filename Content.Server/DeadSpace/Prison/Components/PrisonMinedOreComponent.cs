namespace Content.Server.DeadSpace.Prison.Components;

/// <summary>
/// Tracks how many units in a stack were mined on the prison planet.
/// </summary>
[RegisterComponent, Access(typeof(PrisonOreSystem))]
public sealed partial class PrisonMinedOreComponent : Component
{
    [ViewVariables]
    public int EligibleUnits;
}
