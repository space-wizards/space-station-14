using Content.Shared.Botany;
using Robust.Client.GameObjects;

namespace Content.Client.Botany;

public sealed class PotencyVisualsSystem : VisualizerSystem<PotencyVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PotencyVisualsComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (args.Component.TryGetData(ProduceVisuals.Potency, out float potency))
        {
            var scale = MathHelper.Lerp(component.MinimumScale, component.MaximumScale, potency / 100);
            sprite.Scale = new Vector2(scale, scale);
        }
    }
}
