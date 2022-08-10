namespace Content.Server.Warps
{
    /// <summary>
    /// Allows ghosts etc to warp to this entity by name.
    /// </summary>
    [RegisterComponent]
    public sealed class WarpPointComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("location")] public string? Location { get; set; }
    }
}
