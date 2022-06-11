using Content.Shared.Flash;

namespace Content.Server.Flash.Components
{
    [ComponentReference(typeof(SharedFlashableComponent))]
    [RegisterComponent]
    public sealed class FlashableComponent : SharedFlashableComponent
    {
    }
}
