using Content.Shared.Ghost;

namespace Content.Client.Ghost
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedGhostComponent))]
    public sealed class GhostComponent : SharedGhostComponent
    {
        public bool IsAttached { get; set; }
    }
}
