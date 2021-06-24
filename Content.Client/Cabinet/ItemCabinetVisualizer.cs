using Content.Shared.Cabinet;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Cabinet
{
    [UsedImplicitly]
    public class ItemCabinetVisualizer : AppearanceVisualizer
    {
        // TODO proper layering
        [DataField("fullState", required: true)]
        private string _fullState = default!;

        [DataField("emptyState", required: true)]
        private string _emptyState = default!;

        [DataField("closedState", required: true)]
        private string _closedState = default!;

        [DataField("closedEmptyState", required: true)]
        private string _closedEmptyState = default!;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Owner.TryGetComponent<SpriteComponent>(out var sprite)
                && component.TryGetData(ItemCabinetVisuals.IsOpen, out bool isOpen)
                && component.TryGetData(ItemCabinetVisuals.ContainsItem, out bool contains))
            {
                if (isOpen)
                {
                    if (contains)
                    {
                        sprite.LayerSetState(0, _fullState);
                    }
                    else
                    {
                        sprite.LayerSetState(0, _emptyState);
                    }
                }
                else

                if (contains)
                {
                    sprite.LayerSetState(0, _closedState);
                }
                else
                {
                    sprite.LayerSetState(0, _closedEmptyState);
                }
            }
        }
    }
}
