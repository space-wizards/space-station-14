namespace Content.Server.Warps
{
    /// <summary>
    /// Allows ghosts etc to warp to this entity by name.
    /// </summary>
    [RegisterComponent]
    public sealed partial class WarpPointComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public string? Location;

        /// <summary>
        /// If true, ghosts warping to this entity will begin following it.
        /// </summary>
        [DataField]
        public bool Follow;

        /// <summary>
        /// Should this warp point be accessable to ghosts only?
        /// Useful where you want things like a ghost to reach only like CentComm
        /// </summary>
        [DataField]
        public bool GhostOnly;
    }
}
