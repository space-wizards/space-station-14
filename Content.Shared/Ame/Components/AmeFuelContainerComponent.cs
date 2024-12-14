using Robust.Shared.GameStates;

namespace Content.Shared.Ame.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AmeFuelContainerComponent : Component
{
    /// <summary>
    /// The amount of fuel in the container.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int FuelAmount = 500;

    /// <summary>
    /// The maximum fuel capacity of the container.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int FuelCapacity = 500;
}
