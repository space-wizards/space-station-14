using System.Threading;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.Implants.Components;
[RegisterComponent]
public sealed class ImplanterComponent : Component
{

    /// <summary>
    /// The implant to be used in the implanter
    /// </summary>
    [DataField("implantPrototype")]
    public string? ImplantPrototype;

    //TODO: Just check the container for the implant. No need to add a string here. Aka use storagefill to put implant in

    public CancellationTokenSource? CancelToken;

    //TODO: Add container to prototype/entity with a whitelist for SubdermalImplants
}
