using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client.GreyStation.Clothing;

public sealed class ShaderClothingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ShaderInstance? Shader;

    private EntityQuery<EyeComponent> _eyeQuery;

    public ShaderClothingOverlay()
    {
        IoCManager.InjectDependencies(this);

        _eyeQuery = _ent.GetEntityQuery<EyeComponent>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_eyeQuery.TryComp(_player.LocalSession?.AttachedEntity, out var eye))
            return false;

        return args.Viewport.Eye == eye.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || Shader is not {} shader)
            return;

        shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var world = args.WorldHandle;
        var viewport = args.WorldBounds;
        world.UseShader(shader);
        world.DrawRect(viewport, Color.White);
        world.UseShader(null);
    }
}
