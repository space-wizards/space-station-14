using System.Numerics;
using Content.Shared.Anomaly;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Anomaly;

/// <inheritdoc cref="SharedAnomalyScannerSystem"/>
public sealed class AnomalyScannerSystem : SharedAnomalyScannerSystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const float MaxHueDegrees = 360f;
    private const float GreenHueDegrees = 110f;
    private const float RedHueDegrees = 0f;
    private const float GreenHue = GreenHueDegrees / MaxHueDegrees;
    private const float RedHue = RedHueDegrees / MaxHueDegrees;


    // Just an array to initialize the pixels of a new OwnedTexture
    private static readonly Rgba32[] EmptyTexture = new Rgba32[32*32];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyScannerScreenComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AnomalyScannerScreenComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<AnomalyScannerScreenComponent, AppearanceChangeEvent>(OnScannerAppearanceChanged);
    }

    private void OnComponentInit(Entity<AnomalyScannerScreenComponent> ent, ref ComponentInit args)
    {
        if(!_sprite.TryGetLayer(ent.Owner, AnomalyScannerVisualLayers.Base, out var layer, true))
            return;

        // Allocate the OwnedTexture
        ent.Comp.ScreenTexture = _clyde.CreateBlankTexture<Rgba32>(layer.PixelSize);

        if (layer.PixelSize.X < ent.Comp.Offset.X + ent.Comp.Size.X ||
            layer.PixelSize.Y < ent.Comp.Offset.Y + ent.Comp.Size.Y)
        {
            // If the bar doesn't fit, just bail here, ScreenTexture and BarBuf will remain null, and appearance updates
            // will do nothing.
            DebugTools.Assert(false, "AnomalyScannerScreenComponent: Bar does not fit within sprite");
            return;
        }


        // Initialize the texture
        ent.Comp.ScreenTexture.SetSubImage((0, 0), layer.PixelSize, new ReadOnlySpan<Rgba32>(EmptyTexture));

        // Initialize bar drawing buffer
        ent.Comp.BarBuf = new Rgba32[ent.Comp.Size.X * ent.Comp.Size.Y];
    }

    private void OnComponentStartup(Entity<AnomalyScannerScreenComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.LayerSetTexture((ent, sprite), AnomalyScannerVisualLayers.Screen, ent.Comp.ScreenTexture);
    }

    private void OnScannerAppearanceChanged(Entity<AnomalyScannerScreenComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is null || ent.Comp.ScreenTexture is null || ent.Comp.BarBuf is null)
            return;

        args.AppearanceData.TryGetValue(AnomalyScannerVisuals.AnomalySeverity, out var severityObj);
        if (severityObj is not float severity)
            severity = 0;

        // Get the bar length
        var barLength = (int)(severity * ent.Comp.Size.X);

        // Calculate the bar color
        // Hue "angle" of two colors to interpolate between depending on severity
        // Just a lerp from Green hue at severity = 0.5 to Red hue at 1.0
        var hue = Math.Clamp(2*GreenHue * (1 - severity), RedHue, GreenHue);
        var color = new Rgba32(Color.FromHsv(new Vector4(hue, 1f, 1f, 1f)).RGBA);

        var transparent = new Rgba32(0, 0, 0, 255);

        for(var y = 0; y < ent.Comp.Size.Y; y++)
        {
            for (var x = 0; x < ent.Comp.Size.X; x++)
            {
                ent.Comp.BarBuf[y*ent.Comp.Size.X + x]  = x < barLength ? color : transparent;
            }
        }

        // Copy the buffer to the texture
        try
        {
            ent.Comp.ScreenTexture.SetSubImage(
                ent.Comp.Offset,
                ent.Comp.Size,
                new ReadOnlySpan<Rgba32>(ent.Comp.BarBuf)
            );
        }
        catch (IndexOutOfRangeException)
        {
            Log.Warning($"Bar dimensions out of bounds with the texture on entity {ent.Owner}");
        }
    }
}
