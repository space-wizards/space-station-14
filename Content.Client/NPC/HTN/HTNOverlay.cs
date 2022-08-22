using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.NPC;

public sealed class HTNOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var (comp, xform) in _entManager.EntityQuery<HTNComponent, TransformComponent>(true))
        {
            if (xform.MapID != args.MapId)
                continue;
        }
    }
}
