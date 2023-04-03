using Content.Shared.Conveyor;
using Content.Shared.Recycling;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Recycling
{
    [UsedImplicitly]
    public sealed class RecyclerVisualizer : AppearanceVisualizer
    {
        [DataField("state_on")]
        private string _stateOn = "grinder-o1";

        [DataField("state_off")]
        private string _stateOff = "grinder-o0";

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent(entity, out SpriteComponent? sprite) ||
                !entMan.TryGetComponent(entity, out AppearanceComponent? appearance))
            {
                return;
            }

            UpdateAppearance(appearance, sprite);
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite))
            {
                return;
            }

            UpdateAppearance(component, sprite);
        }

        private void UpdateAppearance(AppearanceComponent component, SpriteComponent sprite)
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
