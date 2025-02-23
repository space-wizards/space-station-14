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
        ///     If true, ghosts warping to this entity will begin following it.
        /// </summary>
        [DataField]
        public bool Follow;

        /// <summary>
        ///     Color of the warp button in the ghost menu.
        /// </summary>
        [DataField]
        public Color Color = new Color(67, 67, 92, 255); // light purple, TODO add purple preset in the robusttoolbox
    }
    
}
