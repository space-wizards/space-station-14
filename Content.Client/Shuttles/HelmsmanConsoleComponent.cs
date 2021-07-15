using Content.Shared.Shuttles;
using Robust.Shared.GameObjects;

namespace Content.Client.Shuttles
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHelmsmanConsoleComponent))]
    internal sealed class HelmsmanConsoleComponent : SharedHelmsmanConsoleComponent
    {

    }
}
