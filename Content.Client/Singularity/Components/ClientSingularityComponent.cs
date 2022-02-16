using Content.Shared.Singularity.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Singularity.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSingularityComponent))]
    public sealed class ClientSingularityComponent : SharedSingularityComponent
    {
    }
}
