using Robust.Shared.GameStates;

namespace Content.Shared.Research.Disk
{
    /// <summary>
    ///     Entity can be redeemed at an R&D server to add research points to that server.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    public sealed partial class ResearchDiskComponent : Component
    {
        /// <summary>
        ///     Points this disk is worth.
        /// </summary>
        [DataField, AutoNetworkedField]
        public int Points = 1000;

        /// <summary>
        /// If true, the value of this disk will be set to the sum
        /// of all the technologies in the game.
        /// </summary>
        /// <remarks>
        /// This is for debug purposes only.
        /// </remarks>
        [DataField]
        public bool UnlockAllTech = false;
    }
}
