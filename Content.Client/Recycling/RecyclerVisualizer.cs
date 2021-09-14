using Content.Shared.Conveyor;
using Content.Shared.Recycling;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Recycling
{
    [UsedImplicitly]
    public class RecyclerVisualizer : AppearanceVisualizer
    {
        [DataField("state_on")]
        private string _stateOn = "grinder-o1";

        [DataField("state_off")]
        private string _stateOff = "grinder-o0";

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out ISpriteComponent? sprite) ||
                !entity.TryGetComponent(out AppearanceComponent? appearance))
            {
                return;
            }

            UpdateAppearance(appearance, sprite);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            UpdateAppearance(component, sprite);
        }

        private void UpdateAppearance(AppearanceComponent component, ISpriteComponent sprite)
        {
            var state = _stateOff;
            if (component.TryGetData(ConveyorVisuals.State, out ConveyorState conveyorState) && conveyorState != ConveyorState.Off)
            {
                state = _stateOn;
            }

            if (component.TryGetData(RecyclerVisuals.Bloody, out bool bloody) && bloody)
            {
                state += "bld";
            }

            sprite.LayerSetState(RecyclerVisualLayers.Main, state);
        }
    }

    public enum RecyclerVisualLayers : byte
    {
        Main
    }
}
