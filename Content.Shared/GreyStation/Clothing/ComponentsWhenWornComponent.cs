using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.GreyStation.Clothing;

/// <summary>
/// Makes clothing add components to the wearer when worn, and remove when taken off.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ComponentsWhenWornSystem))]
public sealed partial class ComponentsWhenWornComponent : Component
{
    /// <summary>
    /// The components to add.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();
}
