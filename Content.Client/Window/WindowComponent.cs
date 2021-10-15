using Content.Shared.Window;
using Robust.Shared.GameObjects;

namespace Content.Client.Window
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedWindowComponent))]
    public class WindowComponent : SharedWindowComponent
    {
    }
}
