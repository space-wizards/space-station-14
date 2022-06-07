namespace Content.Server.Remotes
{
    [RegisterComponent]
    [Friend(typeof(DoorRemoteSystem))]
    public sealed class DoorRemoteComponent : Component
    {
        public OperatingMode Mode = OperatingMode.OpenClose;

        public enum OperatingMode : byte
        {
            OpenClose,
            ToggleBolts,
            ToggleEmergencyAccess
        }
    }
}
