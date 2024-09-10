using Content.Client.Movement.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Eye;

namespace Content.Client.Overlays;

public sealed class DarkenedVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _circleMaskShader;

    public DarkenedVisionComponent? DarkenedVision;


    public DarkenedVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _circleMaskShader = _prototypeManager.Index<ShaderPrototype>("CircleMask").InstanceUnique();
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

        return DarkenedVision != null && DarkenedVision.Strength < DarkenedVision.BlindTreshold && DarkenedVision.Strength > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return;

        if (_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var content))
        {
            _circleMaskShader?.SetParameter("Zoom", content.Zoom.X);
        }

        _circleMaskShader?.SetParameter("CirclePow", 1f);
        _circleMaskShader?.SetParameter("CircleRadius", (10 - DarkenedVision!.Strength) * 32f);

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        worldHandle.UseShader(_circleMaskShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
