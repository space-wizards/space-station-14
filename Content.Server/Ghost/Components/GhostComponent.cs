using System;
using Content.Shared.Ghost;
using Robust.Shared.GameObjects;

#nullable enable
namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent
    {
        [DataField("canInteract")]
        public TimeSpan TimeOfDeath { get; set; } = TimeSpan.Zero;
    }
}
