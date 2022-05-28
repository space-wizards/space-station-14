namespace Content.Shared.Vehicle.Components
{
    /// <summary>
    /// Added to people when they are riding in a vehicle
    /// used mostly to keep track of them for entityquery.
    /// </summary>
    [RegisterComponent]
    public sealed class RiderComponent : Component
    {
        /// <summary>
        /// The vehicle this rider is currently riding.
        /// </summary>
        [ViewVariables] public VehicleComponent? Vehicle;
    }
}
