using System;
using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Atmos
{
    [UsedImplicitly]
    public class PipeConnectorVisualizer : AppearanceVisualizer
    {
        private string _baseState;

        private string _rsiString;

        private RSI _rsi;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var serializer = YamlObjectSerializer.NewReader(node);
            serializer.DataField(ref _baseState, "baseState", "pipeConnector");
            serializer.DataField(ref _rsiString, "rsi", "Constructible/Atmos/pipe.rsi");

            var rsiPath = SharedSpriteComponent.TextureRoot / _rsiString;
            try
            {
                var resourceCache = IoCManager.Resolve<IResourceCache>();
                var resource = resourceCache.GetResource<RSIResource>(rsiPath);
                _rsi = resource.RSI;
            }
            catch (Exception e)
            {
                Logger.ErrorS("go.ventvisualizer", "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, e);
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out ISpriteComponent sprite))
                return;

            foreach (Layer layerKey in Enum.GetValues(typeof(Layer)))
            {
                sprite.LayerMapReserveBlank(layerKey);
                var layer = sprite.LayerMapGet(layerKey);
                sprite.LayerSetRSI(layer, _rsi);
                var layerState = _baseState + ((PipeDirection) layerKey).ToString();
                sprite.LayerSetState(layer, layerState);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
                return;

            if (!component.TryGetData(PipeVisuals.VisualState, out PipeVisualState state))
                return;

            foreach (Layer layerKey in Enum.GetValues(typeof(Layer)))
            {
                var dir = (PipeDirection) layerKey;
                var layerVisible = state.ConnectedDirections.HasDirection(dir);

                var layer = sprite.LayerMapGet(layerKey);
                sprite.LayerSetVisible(layer, layerVisible);
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
