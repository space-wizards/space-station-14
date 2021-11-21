using System.Collections.Generic;
using Content.Shared.Shuttles;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedShuttleConsoleComponent))]
    internal sealed class ShuttleConsoleComponent : SharedShuttleConsoleComponent
    {
        [ViewVariables]
        public List<PilotComponent> SubscribedPilots = new();

        /// <summary>
        /// Whether the console can be used to pilot. Toggled whenever it gets powered / unpowered.
        /// </summary>
        [ViewVariables]
        public bool Enabled { get; set; } = false;
    }
}
