namespace Content.Server.Forensics
{
    /// <summary>
    /// This controls fibers left by backpacks and pockets of clothes on stored items,
    /// which the forensics system uses.
    /// </summary>
    [RegisterComponent]
    public sealed partial class MicroFiberComponent : Component
    {
        [DataField]
        public LocId MicroFiberMaterial = "fibers-synthetic";

        [DataField]
        public string? MicroFiberColor;
    }
}
