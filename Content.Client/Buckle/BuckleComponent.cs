using Content.Shared.Buckle.Components;

namespace Content.Client.Buckle
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBuckleComponent))]
    public sealed class BuckleComponent : SharedBuckleComponent
    {
        public int? OriginalDrawDepth { get; set; }
    }
}
