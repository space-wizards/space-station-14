using Content.Shared.Shuttles;
using Robust.Shared.GameObjects;

namespace Content.Client.Shuttles
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedShuttleConsoleComponent))]
    internal sealed class ShuttleConsoleComponent : SharedShuttleConsoleComponent
    {

    }
}
