using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.CF.Combat;

/// <summary>
/// Draws floating Pathfinder combat numbers in screen space after the world.
/// </summary>
public sealed class CFDamageNumberOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    private readonly CFDamageNumberSystem _numbers;
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public CFDamageNumberOverlay(CFDamageNumberSystem numbers)
    {
        _numbers = numbers;
        _shader = IoCManager.Resolve<IPrototypeManager>().Index(UnshadedShader).Instance();
        ZIndex = 6;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        args.DrawingHandle.SetTransform(Matrix3x2.Identity);
        args.DrawingHandle.UseShader(_shader);
        _numbers.Draw(args.ScreenHandle, args, _numbers.UiScale());
        args.DrawingHandle.UseShader(null);
    }
}
