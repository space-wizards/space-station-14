using Content.Shared.Botany;
using Content.Client.Botany.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Botany;

public sealed class PotencyVisualsSystem : VisualizerSystem<PotencyVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PotencyVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.Component.TryGetData(ProduceVisuals.Potency, out float potency))
        {
            var scale = MathHelper.Lerp(component.MinimumScale, component.MaximumScale, potency / 100);
            args.Sprite.Scale = new Vector2(scale, scale);
        }
    }
}
