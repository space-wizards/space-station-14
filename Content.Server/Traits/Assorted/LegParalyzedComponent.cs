namespace Content.Server.Traits.Assorted;

/// <summary>
/// Set player speed to zero and standing state to down, simulating leg paralysis.
/// Used for Wheelchair bound trait.
/// </summary>
[RegisterComponent, Access(typeof(LegParalyzedSystem))]
public sealed class LegParalyzedComponent : Component
{
}
