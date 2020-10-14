using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedWindowComponent))]
    public class WindowComponent : SharedWindowComponent
    {
    }
}
