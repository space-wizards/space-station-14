using Robust.Shared.GameStates;

namespace Content.Shared.Machines.Components;

/// <summary>
/// Component for marking entities as part of a multipart machine.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultipartMachinePartComponent : Component
{
    /// <summary>
    /// Links to the entity which holds the MultipartMachineComponent.
    /// Useful so that entities that know which machine they are a part of.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Master = null;
}
