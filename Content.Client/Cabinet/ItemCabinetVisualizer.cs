using Content.Shared.Cabinet;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Cabinet
{
    [UsedImplicitly]
    public class ItemCabinetVisualizer : AppearanceVisualizer
    {
        [DataField("openState", required: true)]
        private string _openState = default!;

        [DataField("closedState", required: true)]
        private string _closedState = default!;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (entities.TryGetComponent(component.Owner, out SpriteComponent sprite)
                && component.TryGetData(ItemCabinetVisuals.IsOpen, out bool isOpen)
                && component.TryGetData(ItemCabinetVisuals.ContainsItem, out bool contains))
            {
                var state = isOpen ? _openState : _closedState;
                sprite.LayerSetState(ItemCabinetVisualLayers.Door, state);
                sprite.LayerSetVisible(ItemCabinetVisualLayers.ContainsItem, contains);
            }
        }
    }

    public enum ItemCabinetVisualLayers : byte
    {
        Door,
        ContainsItem
        //Welded
    }
}
