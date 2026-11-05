using System.Numerics;
using Content.Shared.Botany;
using Content.Client.Botany.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Botany;

public sealed partial class PotencyVisualsSystem : VisualizerSystem<PotencyVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PotencyVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData<float>(ProduceVisuals.Potency, out var potency))
            return;

        var potencyRatio = Math.Clamp(potency / 100f, 0f, 1f);
        var scale = MathHelper.Lerp(component.MinimumScale, component.MaximumScale, potencyRatio);
        SpriteSystem.SetScale((uid, args.Sprite), new Vector2(scale, scale));
    }
}
