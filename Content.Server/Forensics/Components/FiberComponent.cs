namespace Content.Server.Forensics
{
    /// <summary>
    /// This controls fibers left by gloves on items,
    /// which the forensics system uses.
    /// </summary>
    [RegisterComponent]
    public sealed partial class FiberComponent : Component
    {
        [DataField]
        public LocId FiberMaterial = "fibers-synthetic";

        [DataField]
        public string? FiberColor;
    }
}
