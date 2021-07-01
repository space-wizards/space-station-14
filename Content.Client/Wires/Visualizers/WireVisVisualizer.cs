using Content.Shared.Wires;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Wires.Visualizers
{
    [DataDefinition]
    public sealed class WireVisVisualizer : AppearanceVisualizer
    {
        [DataField("base")]
        public string? StateBase;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out SpriteComponent? sprite))
                return;

            if (!component.TryGetData(WireVisVisuals.ConnectedMask, out WireVisDirFlags mask))
                mask = WireVisDirFlags.None;

            sprite.LayerSetState(0, $"{StateBase}{(int) mask}");
        }
    }
}
