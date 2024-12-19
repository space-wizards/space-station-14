namespace Content.Server.Drone.Components
{
    [RegisterComponent]
    public sealed partial class DroneComponent : Component
    {
        public float InteractionBlockRange = 1.5f; /// imp. original value was 2.15, changed because it was annoying. this also does not actually block interactions anymore.
		
		// imp. delay before posting another proximity alert
		public TimeSpan ProximityDelay = TimeSpan.FromMilliseconds(2000);
		
		public TimeSpan NextProximityAlert = new();
		
		public EntityUid NearestEnt = default!;
    }
}
