namespace Content.Server.Research.Disk
{
    [RegisterComponent]
    public sealed class ResearchDiskComponent : Component
    {
        [DataField("points"), ViewVariables(VVAccess.ReadWrite)]
        public int Points = 1000;

        /// <summary>
        /// If true, the value of this disk will be set to the sum
        /// of all the technologies in the game.
        /// </summary>
        /// <remarks>
        /// This is for debug purposes only.
        /// </remarks>
        [DataField("unlockAllTech")]
        public bool UnlockAllTech = false;
    }
}
