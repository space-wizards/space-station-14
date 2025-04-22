namespace Content.Server.Machines.Components;

/// <summary>
/// Server side component for marking entities as part of a multipart machine.
/// </summary>
[RegisterComponent]
public sealed partial class MultipartMachinePartComponent : Component
{
    /// <summary>
    /// Links to the entity which holds the MultipartMachineComponent.
    /// Useful so that entities that know which machine they are a part of.
    /// </summary>
    [DataField]
    public EntityUid? Master = null;
}
