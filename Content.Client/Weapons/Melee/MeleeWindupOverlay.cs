using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Weapons.Melee;

public sealed class MeleeWindupOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        // throw new NotImplementedException();
    }
}
