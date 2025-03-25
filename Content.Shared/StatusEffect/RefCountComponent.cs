namespace Content.Shared.StatusEffect;

/// <summary>
/// Refcounted components that get only get removed when its sources count goes to 0.
/// Use this instead of blindly adding/removing components (related: https://youtu.be/k0MUj34y5Kg)
/// </summary>
[RegisterComponent, Access(typeof(RefCountSystem))]
public sealed partial class RefCountComponent : Component
{
    /// <summary>
    /// Each component added and the number of sources it has.
    /// </summary>
    /// <remarks>
    /// Not networked because dear god that would be complicated.
    /// </remarks>
    [DataField]
    public Dictionary<Type, uint> Counts = new();
}
