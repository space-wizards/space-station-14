using Content.Shared.Shuttles;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Shuttles
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedShuttleConsoleComponent))]
    internal sealed class ShuttleConsoleComponent : SharedShuttleConsoleComponent
    {

    }
}
