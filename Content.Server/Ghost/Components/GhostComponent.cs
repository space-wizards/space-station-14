using System;
using Content.Shared.Ghost;
using Robust.Shared.GameObjects;

#nullable enable
namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent
    {
        public TimeSpan TimeOfDeath { get; set; } = TimeSpan.Zero;
    }
}
