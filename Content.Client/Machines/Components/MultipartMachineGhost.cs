namespace Content.Client.Machines.Components;

// Component attached to all multipart machine ghosts
[RegisterComponent]
public sealed partial class MultipartMachineGhostComponent : Component
{
    /// <summary>
    /// Machine this particular ghost is linked to.
    /// </summary>
    public EntityUid? LinkedMachine = null;
}
