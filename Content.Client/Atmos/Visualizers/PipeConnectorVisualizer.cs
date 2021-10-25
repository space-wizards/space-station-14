using System;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.SubFloor;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public class PipeConnectorVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        [DataField("rsi")]
        private string _rsi = "Structures/Piping/Atmospherics/pipe.rsi";

        [DataField("baseState")]
        private string _baseState = "pipeConnector";

        private RSI? _connectorRsi;

        void ISerializationHooks.AfterDeserialization()
        {
            var rsiString = SharedSpriteComponent.TextureRoot / _rsi;
            var resourceCache = IoCManager.Resolve<IResourceCache>();

            if (resourceCache.TryGetResource(rsiString, out RSIResource? rsi))
                _connectorRsi = rsi.RSI;
            else
                Logger.Error($"{nameof(PipeConnectorVisualizer)} could not load to load RSI {rsiString}.");
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent<ISpriteComponent>(out var sprite))
                return;

            if (_connectorRsi == null)
                return;

            foreach (Layer layerKey in Enum.GetValues(typeof(Layer)))
            {
                sprite.LayerMapReserveBlank(layerKey);
                var layer = sprite.LayerMapGet(layerKey);
                sprite.LayerSetRSI(layer, _connectorRsi);
                var layerState = _baseState + ((PipeDirection) layerKey).ToString();
                sprite.LayerSetState(layer, layerState);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<ITransformComponent>(out var xform))
                return;

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite))
                return;

            if (!component.TryGetData(PipeColorVisuals.Color, out Color color))
                color = Color.White;

            if (!component.TryGetData(PipeVisuals.VisualState, out PipeVisualState state))
                return;

            if(!component.TryGetData(SubFloorVisuals.SubFloor, out bool subfloor))
                subfloor = true;

            var rotation = xform.LocalRotation;

            foreach (Layer layerKey in Enum.GetValues(typeof(Layer)))
            {
                var layer = sprite.LayerMapGet(layerKey);
                var dir = (PipeDirection) layerKey;
                var visible = subfloor && state.ConnectedDirections.HasDirection(dir);
                sprite.LayerSetVisible(layer, visible);

                if (!visible) continue;

                sprite.LayerSetRotation(layer, -rotation);
                sprite.LayerSetColor(layer, color);
            }
        }

        private enum Layer : byte
        {
            NorthConnection = PipeDirection.North,
            SouthConnection = PipeDirection.South,
            EastConnection = PipeDirection.East,
            WestConnection = PipeDirection.West,
        }
    }
}
