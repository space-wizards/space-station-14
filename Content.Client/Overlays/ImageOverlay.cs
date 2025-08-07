using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client.Overlays;

/// <summary>
/// Creates overlay image placed over user screen
/// </summary>
public sealed partial class ImageOverlay : Overlay
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public readonly List<(ShaderInstance, ImageShaderValues)> TupleOfImageShaders = new();

    public ImageOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var (shaderInstance, shaderValues) in TupleOfImageShaders)
        {
            var handle = args.WorldHandle;

            shaderInstance.SetParameter("OverlayTexture", _resourceCache.GetTexture(shaderValues.ResPath));
            shaderInstance.SetParameter("AdditionalColor", shaderValues.AdditionalColorOverlay);

            handle.UseShader(shaderInstance);
            handle.DrawRect(args.WorldBounds, Color.White);
            handle.UseShader(null);
        }
    }
}

public struct ImageShaderValues()
{
    public string ResPath = "";
    public Color AdditionalColorOverlay = new(0, 0, 0, 0);
}
