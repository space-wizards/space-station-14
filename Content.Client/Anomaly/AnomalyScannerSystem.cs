using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Anomaly;

public sealed class AnomalyScannerSystem : SharedAnomalyScannerSystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _log = default!;

    // Just an array to initialize the pixels of a new OwnedTexture
    private static readonly Rgba32[] EmptyTexture = new Rgba32[32*32];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyScannerComponent, ComponentInit>(OnScannerInit);
        SubscribeLocalEvent<AnomalyScannerScreenComponent, ComponentInit>(OnScannerScreenInit);
        SubscribeLocalEvent<AnomalyScannerScreenComponent, AppearanceChangeEvent>(OnScannerAppearanceChanged);
    }

    private void OnScannerInit(Entity<AnomalyScannerComponent> ent, ref ComponentInit args)
    {
        EnsureComp<AnomalyScannerScreenComponent>(ent);
    }

    private void OnScannerScreenInit(Entity<AnomalyScannerScreenComponent> ent, ref ComponentInit args)
    {
        // Allocate the OwnedTexture
        ent.Comp.ScreenTexture ??= _clyde.CreateBlankTexture<Rgba32>((32, 32));
        // Initialize the texture
        ent.Comp.ScreenTexture.SetSubImage((0, 0), (32, 32), new ReadOnlySpan<Rgba32>(EmptyTexture));

        // Initialize bar drawing buffer
        ent.Comp.BarBuf ??= new Rgba32[ent.Comp.Size.X * ent.Comp.Size.Y];

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var spriteSystem = EntityManager.System<SpriteSystem>();
        spriteSystem.LayerSetTexture((ent, sprite), AnomalyScannerVisualLayers.Screen, ent.Comp.ScreenTexture);
    }

    private void OnScannerAppearanceChanged(Entity<AnomalyScannerScreenComponent> ent, ref AppearanceChangeEvent args)
    {
        args.AppearanceData.TryGetValue(AnomalyScannerVisuals.AnomalySeverity, out var severityObj);
        if (severityObj is not float severity)
            severity = 0;

        if (!HasComp<SpriteComponent>(ent))
            return;

        ent.Comp.ScreenTexture ??= _clyde.CreateBlankTexture<Rgba32>((32, 32));

        if(ent.Comp.BarBuf?.Length != ent.Comp.Size.X * ent.Comp.Size.Y)
            ent.Comp.BarBuf = new Rgba32[ent.Comp.Size.X * ent.Comp.Size.Y];

        // Get the bar length
        var barLength = (int)(severity * ent.Comp.Size.X);

        // Calculate the bar color
        // Hue "angle" of two colors to interpolate between depending on severity
        const float greenHue = 110f / 360f;
        const float redHue = 0f;
        // Just a lerp from greenHue at severity = 0.5 to redHue at 1.0
        var hue = Math.Clamp(2*greenHue * (1 - severity), redHue, greenHue);
        var color = new Rgba32(Color.FromHsv((hue, 1, 1, 1)).RGBA);

        var transparent = new Rgba32(0, 0, 0, 255);

        for(var y = 0; y < ent.Comp.Size.Y; y++)
        {
            for (var x = 0; x < ent.Comp.Size.X; x++)
            {
                ent.Comp.BarBuf[y*10 + x]  = x < barLength ? color : transparent;
            }
        }

        // Copy the buffer to the texture
        try
        {
            ent.Comp.ScreenTexture.SetSubImage(ent.Comp.Offset,
                ent.Comp.Size,
                new ReadOnlySpan<Rgba32>(ent.Comp.BarBuf));
        }
        catch (IndexOutOfRangeException)
        {
            Log.Warning("Bar dimensions out of bounds with the texture");
        }
    }

}
