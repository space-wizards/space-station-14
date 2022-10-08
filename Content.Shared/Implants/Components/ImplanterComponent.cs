using System.Threading;

namespace Content.Shared.Implants.Components;
[RegisterComponent]
public sealed class ImplanterComponent : Component
{

    //TODO: Just check the container for the implant. No need to add a string here.

    public CancellationTokenSource? CancelToken;

    //TODO: Add container to prototype/entity with a whitelist for SubdermalImplants
}
