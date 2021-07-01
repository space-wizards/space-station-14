using Content.Shared.Wires;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Power
{
    [DataDefinition]
    public sealed class CableVisualizer : AppearanceVisualizer
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
