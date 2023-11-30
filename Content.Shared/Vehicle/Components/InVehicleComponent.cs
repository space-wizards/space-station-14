using Robust.Shared.GameStates;

namespace Content.Shared.Vehicle.Components
{
    /// <summary>
    /// Added to objects inside a vehicle to stop people besides the rider from
    /// removing them.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class InVehicleComponent : Component
    {
        /// <summary>
        /// The vehicle this rider is currently riding.
        /// </summary>
        [ViewVariables] public VehicleComponent? Vehicle;
    }
}
