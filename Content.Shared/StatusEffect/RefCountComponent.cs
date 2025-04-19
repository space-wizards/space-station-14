using Robust.Shared.GameStates;

namespace Content.Shared.StatusEffect;

/// <summary>
/// Refcounted components that get only get removed when its sources count goes to 0.
/// Use this instead of blindly adding/removing components (related: https://youtu.be/k0MUj34y5Kg)
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RefCountSystem))]
[AutoGenerateComponentState]
public sealed partial class RefCountComponent : Component
{
    /// <summary>
    /// Each component name added and the number of sources it has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, uint> Counts = new();
}
