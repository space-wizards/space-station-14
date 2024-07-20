using Content.Shared.NPC.NuPC;
using Robust.Client.Graphics;

namespace Content.Client.NPC.NuPC;


public sealed class NpcGoalSystem : SharedNpcGoalSystem
{
    [Dependency] private readonly IOverlayManager _overlays = default!;

    private bool _enabled;

    public bool Enabled { get; set; }
}
