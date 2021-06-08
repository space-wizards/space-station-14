using System;
using Content.Shared.GameObjects.Components.Observer;
using Robust.Shared.GameObjects;

#nullable enable
namespace Content.Server.GameObjects.Components.Observer
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent
    {
        public TimeSpan TimeOfDeath { get; set; } = TimeSpan.Zero;
    }
}
