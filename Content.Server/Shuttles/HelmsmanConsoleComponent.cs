using System.Collections.Generic;
using Content.Shared.Shuttles;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Shuttles
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHelmsmanConsoleComponent))]
    internal sealed class HelmsmanConsoleComponent : SharedHelmsmanConsoleComponent
    {
        [ViewVariables]
        public List<PilotComponent> SubscribedPilots = new();
    }
}
