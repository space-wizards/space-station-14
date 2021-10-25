using Content.Shared.Atmos.Piping.Unary.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public class GasPortableVisualizer : AppearanceVisualizer
    {
        [DataField("stateConnected")]
        private string? _stateConnected;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            if (_stateConnected != null)
            {
                sprite.LayerMapSet(Layers.ConnectedToPort, sprite.AddLayerState(_stateConnected));
                sprite.LayerSetVisible(Layers.ConnectedToPort, false);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            // Update the visuals : Is the canister connected to a port or not
            if (component.TryGetData(GasPortableVisuals.ConnectedState, out bool isConnected))
            {
                sprite.LayerSetVisible(Layers.ConnectedToPort, isConnected);
            }
        }

        private enum Layers : byte
        {
            ConnectedToPort,
        }
    }
}
