namespace Content.Server.Forensics;

/// <summary>
/// This controls fibers left by backpacks and pockets of clothes on stored items,
/// which the forensics system uses.
/// </summary>
[RegisterComponent]
public sealed partial class MicroFiberComponent : Component
{
    /// <summary>
    /// Locale id for the microfiber material of this item
    /// </summary>
    [DataField]
    public LocId MicroFiberMaterial = "micro-fibers-synthetic";

    /// <summary>
    /// Microfiber color if needed
    /// </summary>
    [DataField]
    public string? MicroFiberColor;
}
