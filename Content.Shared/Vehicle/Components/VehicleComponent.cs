namespace Content.Shared.Vehicle.Components
{
    /// <summary>
    /// This is particularly for vehicles that use
    /// buckle. Stuff like clown cars may need a different
    /// component at some point.
    /// All vehicles should have Physics, Strap, and SharedPlayerInputMover components.
    /// </summary>
    [RegisterComponent]
    public sealed class VehicleComponent : Component
    {
        /// <summary>
        /// Whether the vehicle currently has a key inside it
        /// </summary>
        public bool HasKey = false;

        /// <summary>
        /// Whether someone is currently riding the vehicle
        /// </summary
        public bool HasRider = false;

        /// <summary>
        /// The prototype for the key
        /// </summary>
        [DataField("key", required: true)]
        public string Key = string.Empty;
    }
}
