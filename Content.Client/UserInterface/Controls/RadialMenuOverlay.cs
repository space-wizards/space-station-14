using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Controls;

public sealed class RadialMenuOverlay : Overlay
{
    [Dependency]private readonly IPrototypeManager _prototypeManager = default!;

    private readonly ShaderInstance _shader;

    /// <inheritdoc />
    public RadialMenuOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("RadialMenu").Instance().Duplicate();
    }

    /// <inheritdoc />
    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(_shader);
        var viewport = args.WorldAABB;
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }

    /// <inheritdoc />
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
}
