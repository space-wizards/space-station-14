using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding.Components;
using Robust.Client.GameObjects;
using Content.Shared.Body.Components;

namespace Content.Client._Starlight.Overlay.Thermal;

public sealed class ThermalVisionOverlay : Robust.Client.Graphics.Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly TransformSystem _transform;
    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _shader;
    public ThermalVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("ThermalVisionShader").InstanceUnique();
        _transform = _entityManager.System<TransformSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return false;

        if (!_entityManager.TryGetComponent<ThermalVisionComponent>(playerEntity, out var blurComp))
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        foreach (var (_, sprite, xform) in _entityManager.EntityQuery<BodyComponent, SpriteComponent, TransformComponent>())
        {
            if(xform.MapID != args.MapId) continue;
            var (position, rotation) = _transform.GetWorldPositionRotation(xform);
            sprite.Render(worldHandle, eyeRotation, rotation, null, position);
        }

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
