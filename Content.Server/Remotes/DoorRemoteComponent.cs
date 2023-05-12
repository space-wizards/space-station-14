namespace Content.Server.Remotes
{
    [RegisterComponent]
    [Access(typeof(DoorRemoteSystem))]
    public sealed class DoorRemoteComponent : Component
    {
        public OperatingMode Mode = OperatingMode.OpenClose;

        public enum OperatingMode : byte
        {
            OpenClose,
            ToggleBolts,
            ToggleEmergencyAccess
        }

        /// <summary>
        /// Can you bolt doors with it
        /// </summary>
        [DataField("allowBolt")]
        public bool AllowBolt = true;

        /// <summary>
        /// Can you allow emergency access with it
        /// </summary>
        [DataField("allowEmergencyAccess")]
        public bool AllowEmergencyAccess = true;

        /// <summary>
        /// Does this tool only interact with firelocks?
        /// </summary>
        [DataField("firelockOnly")]
        public bool FirelockOnly = false;

        /// <summary>
        /// Does this tool only interact with airlocks (which are most doors)?
        /// </summary>
        [DataField("airlockOnly")]
        public bool AirlockOnly = true;
    }
}
