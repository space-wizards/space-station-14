namespace Content.Shared.Vehicle.Components
{
    /// <summary>
    /// Lets this item unlock vehicles
    /// that accept its key prototype.
    /// </summary>
    [RegisterComponent]
    public sealed class VehicleKeyComponent : Component
    {
        /// <summary>
        /// The key types this key can unlock
        /// </summary>
        [DataField("keys")]
        public string[] Keys = {};
    }
}
