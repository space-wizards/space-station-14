using System;
using Content.Shared.Ghost;
using Robust.Shared.GameObjects;

namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedGhostComponent))]
    public sealed class GhostComponent : SharedGhostComponent
    {
        public TimeSpan TimeOfDeath { get; set; } = TimeSpan.Zero;
    }
}
