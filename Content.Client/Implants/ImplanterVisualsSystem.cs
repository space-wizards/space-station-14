using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Implants;

public sealed class ImplanterVisualsSystem : VisualizerSystem<ImplanterVisualsComponent>
{


    protected override void OnAppearanceChange(EntityUid uid, ImplanterVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (args.Sprite != null)
            UpdateAppearance(uid, component, args.Sprite, appearance);
    }

    private void UpdateAppearance(EntityUid uid, ImplanterVisualsComponent component, SpriteComponent sprite, AppearanceComponent appearance)
    {
        AppearanceSystem.TryGetData(uid, ImplanterVisuals.Color, out var color, appearance);
        if (color == null)
        {
            Log.Warning($"ImplanterVisualsSystem: No color data found for {uid}");
            return;
        }
        UpdateSprite(sprite, (Color)color);
        Log.Info($"ImplanterVisualsSystem: Updated color to {color} for {uid}");
    }

    private void UpdateSprite(SpriteComponent spriteComponent, Color color)
    {
        spriteComponent.LayerSetColor("implantFull", color);
    }
}
