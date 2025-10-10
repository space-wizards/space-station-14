using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Maths;
using Robust.Client.Player;
using Robust.Shared.GameObjects;

namespace Content.Client.Overlays;

public sealed partial class DesertMirageOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "DesertMirage";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;

    // Tunables with sensible defaults
    public float Strength { get; set; } = 0.6f;
    public float Speed { get; set; } = 1.0f;
    public float DistortScale { get; set; } = 1.0f;
    public float VerticalBias { get; set; } = 0.6f;

    private float _zoom = 1.0f;

    public DesertMirageOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(ShaderId).InstanceUnique();
        ZIndex = 9;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        // Try set zoom from player's eye if available
        var player = _player.LocalSession?.AttachedEntity;
        if (player != null && _entMan.TryGetComponent<EyeComponent>(player, out var eye))
            _zoom = eye.Zoom.X;
        else
            _zoom = 1.0f;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("Strength", Strength);
        _shader.SetParameter("Speed", Speed);
        _shader.SetParameter("DistortScale", DistortScale);
        _shader.SetParameter("VerticalBias", VerticalBias);
        _shader.SetParameter("Zoom", _zoom);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
