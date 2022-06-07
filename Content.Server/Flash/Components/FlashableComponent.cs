using Content.Shared.Flash;

namespace Content.Server.Flash.Components
{
    [ComponentReference(typeof(SharedFlashableComponent))]
    [RegisterComponent, Friend(typeof(FlashSystem))]
    public sealed class FlashableComponent : SharedFlashableComponent
    {
    }
}
