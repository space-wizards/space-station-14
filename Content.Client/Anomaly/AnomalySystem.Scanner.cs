using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Anomaly;

public sealed partial class AnomalySystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    private void InitializeScanner()
    {
        SubscribeLocalEvent<AnomalyScannerComponent, ComponentInit>(OnScannerInit);
        SubscribeLocalEvent<AnomalyScannerScreenComponent, ComponentInit>(OnScannerScreenInit);
        SubscribeLocalEvent<AnomalyScannerScreenComponent, AppearanceChangeEvent>(OnScannerAppearanceChanged);
    }

    private void OnScannerInit(Entity<AnomalyScannerComponent> ent, ref ComponentInit args)
    {
        AddComp<AnomalyScannerScreenComponent>(ent);
    }

    private void OnScannerScreenInit(Entity<AnomalyScannerScreenComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.ScreenTexture is null)
        {
            ent.Comp.ScreenTexture = _clyde.CreateBlankTexture<Rgba32>((32, 32));
            ent.Comp.ScreenTexture.SetSubImage((0, 0), (32, 32), new ReadOnlySpan<Rgba32>(new Rgba32[32*32]));
        }

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;
        sprite.LayerSetTexture(AnomalyScannerVisualLayers.Screen, ent.Comp.ScreenTexture);
    }

    private void OnScannerAppearanceChanged(Entity<AnomalyScannerScreenComponent> ent, ref AppearanceChangeEvent args)
    {
        args.AppearanceData.TryGetValue(AnomalyScannerVisuals.AnomalySeverity, out var severityObj);
        if (severityObj is not float severity)
            severity = 0;

        // var uid = GetEntity(args.Entity);

        if (!HasComp<SpriteComponent>(ent))
            return;

        var screen = EnsureComp<AnomalyScannerScreenComponent>(ent);

        if (screen.ScreenTexture is null)
            return;


        var buf = new Rgba32[50];
        for(var y = 0; y < 5; y++)
        {
            for (var x = 0; x < 10; x++)
            {
                if (y is >= 1 and <= 3)
                {
                    const float greenHue = 110f / 360f;
                    const float redHue = 0f;
                    var hue = Math.Clamp(2*greenHue * (1 - severity), redHue, greenHue);
                    var sev = (int)(severity * 10);
                    var color = new Rgba32(Robust.Shared.Maths.Color.FromHsv((hue, 1, 1, 1)).RGBA);
                    buf[y*10 + x]  = x < sev ? color : new Rgba32(0,0,0,255);
                }
                else
                {
                    buf[y*10 + x] = new Rgba32(0,0,0, 255);
                }
            }
        }
        screen.ScreenTexture.SetSubImage(screen.Offset, (10, 5), new ReadOnlySpan<Rgba32>(buf));

        // sprite.LayerSetTexture(AnomalyScannerVisualLayers.Screen, tex);
        // sprite.LayerSetVisible(AnomalyScannerVisualLayers.Screen, true);

        // _appearance.SetData(uid, AnomalyScannerVisuals.AnomalyState, args.Stability);
    }

    // private void OnScannerAppearanceChanged(Entity<AnomalyScannerScreenComponent> ent, ref AppearanceChangeEvent args)
    // {
    //     if (!args.AppearanceData.TryGetValue(AnomalyScannerVisuals.AnomalyState, out var state))
    //         return;
    //     if (state is not AnomalyStabilityVisuals newVisual)
    //         return;
    //     _appearance.SetData(ent, AnomalyScannerVisuals.AnomalyState, newVisual);
    // }


    // private void FrameUpdateAnomalyScanner(float frameTime)
    // {
    //     throw new NotImplementedException();
    // }
}
