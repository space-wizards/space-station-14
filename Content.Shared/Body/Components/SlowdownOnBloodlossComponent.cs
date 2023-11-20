using System.ComponentModel.DataAnnotations;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

/// <summary>
/// Any entity with this component will experience slowdowns at certain levels of blood.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlowdownOnBloodlossComponent : Component
{

    /// <summary>
    /// The thresholds of blood required to change the entity's speed. First value is threshold and second value is speed multiplier.
    /// </summary>
    /// <returns></returns>
    [DataField(required: true)]
    public Dictionary<float, float> Thresholds = new();

    [AutoNetworkedField]
    public float CurrentMultiplier;
}
