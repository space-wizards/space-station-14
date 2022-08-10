using Content.Shared.Flash;

namespace Content.Server.Flash.Components
{
    [ComponentReference(typeof(SharedFlashableComponent))]
    [RegisterComponent, Access(typeof(FlashSystem))]
    public sealed class FlashableComponent : SharedFlashableComponent
    {
    }
}
