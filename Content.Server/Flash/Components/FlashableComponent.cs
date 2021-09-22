using Content.Shared.Flash;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Flash.Components
{
    [ComponentReference(typeof(SharedFlashableComponent))]
    [RegisterComponent, Friend(typeof(FlashSystem))]
    public sealed class FlashableComponent : SharedFlashableComponent
    {
    }
}
