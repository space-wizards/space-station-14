using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.CF.Combat;

/// <summary>
/// Screen-space debug label for the off-guard condition.
/// </summary>
public sealed class CFOffGuardLabelOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    private readonly CFOffGuardLabelSystem _labels;
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public CFOffGuardLabelOverlay(CFOffGuardLabelSystem labels)
    {
        _labels = labels;
        _shader = IoCManager.Resolve<IPrototypeManager>().Index(UnshadedShader).Instance();
        ZIndex = 5;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        args.DrawingHandle.SetTransform(Matrix3x2.Identity);
        args.DrawingHandle.UseShader(_shader);
        _labels.Draw(args.ScreenHandle, args, _labels.UiScale());
        args.DrawingHandle.UseShader(null);
    }
}
