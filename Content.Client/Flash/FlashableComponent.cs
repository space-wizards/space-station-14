using Content.Shared.Flash;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Client.Flash
{
    [ComponentReference(typeof(SharedFlashableComponent))]
    [RegisterComponent]
    public sealed class FlashableComponent : SharedFlashableComponent
    {
    }
}
