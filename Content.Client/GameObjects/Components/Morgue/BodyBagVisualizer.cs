#nullable enable
using Content.Shared.GameObjects.Components.Morgue;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.Morgue
{
    [UsedImplicitly]
    public sealed class BodyBagVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            if (component.TryGetData(BodyBagVisuals.Label, out bool labelVal))
            {
                sprite.LayerSetVisible(BodyBagVisualLayers.Label, labelVal);
            }
        }
    }

    public enum BodyBagVisualLayers : byte
    {
        Label,
    }
}
