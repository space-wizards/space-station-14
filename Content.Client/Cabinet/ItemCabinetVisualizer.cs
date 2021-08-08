using Content.Shared.Cabinet;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
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

        [DataField("state", required: true)]
        private string _baseState = default!;

        [DataField("openState", required: true)]
        private string _openState = default!;

        [DataField("closedState", required: true)]
        private string _closedState = default!;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Owner.TryGetComponent<SpriteComponent>(out var sprite)
                && component.TryGetData(ItemCabinetVisuals.IsOpen, out bool isOpen)
                && component.TryGetData(ItemCabinetVisuals.ContainsItem, out bool contains))
            {
                sprite.LayerSetState(0, _baseState);

                var state = isOpen ? _openState : _closedState;
                sprite.LayerSetState(ItemCabinetVisualLayers.Door, state);

                if (isOpen)
                {
                    if (contains)
                    {
                        sprite.LayerSetState(ItemCabinetVisuals.ContainsItem, _fullState);
                    }
                    else
                    {
                        sprite.LayerSetState(ItemCabinetVisuals.ContainsItem, _emptyState);
                    }
                }
                else

                sprite.LayerSetState(0, _closedState);
            }
        }
    }

    public enum ItemCabinetVisualLayers : byte
    {
        Door
        //Welded
    }
}
