using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using System.Numerics;

namespace Content.Client.Overlays;

/// <summary>
/// Creates overlay image placed over user screen
/// </summary>
public sealed partial class ImageOverlay : Overlay
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public readonly List<(ShaderInstance, ImageShaderValues)> ImageShaders = new();

    public ImageOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return;

        // TODO check if needed
        if (!_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var eye))
            return;

        foreach (var (shader, values) in ImageShaders)
        {
            var handle = args.WorldHandle;

            var texture = _resourceCache.GetTexture("/Textures/weldingTexture.png");
            shader.SetParameter("OverlayTexture", texture);

            Color color = values.AdditionalColor;
            color.A = values.AdditionalOverlayAlpha;
            shader.SetParameter("AdditionalColor", color);

            handle.UseShader(shader);
            handle.DrawRect(args.WorldBounds, Color.White);
            handle.UseShader(null);
        }
    }
}

public struct ImageShaderValues()
{
    public string PathToOverlayImage = "";
    public float AdditionalOverlayAlpha = 0;
    public Color AdditionalColor = Color.Black;
}
