namespace Content.Server.Forensics
{
    /// <summary>
    /// This controls fibers left by gloves on items,
    /// which the forensics system uses.
    /// </summary>
    [RegisterComponent]
    public sealed partial class FiberComponent : Component
    {
        [DataField("fiberMaterial")]
        public string FiberMaterial = "fibers-synthetic";

        [DataField("fiberColor")]
        public string? FiberColor;
    }
}
