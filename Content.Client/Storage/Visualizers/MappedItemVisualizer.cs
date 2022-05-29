using System.Collections.Generic;
using System.Linq;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Client.Storage.Visualizers
{
    [UsedImplicitly]
    public sealed class MappedItemVisualizer : AppearanceVisualizer
    {
        [DataField("sprite")] private ResourcePath? _rsiPath;
        private List<string> _spriteLayers = new();

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<ISpriteComponent?>(entity, out var spriteComponent))
            {
                _rsiPath ??= spriteComponent.BaseRSI!.Path!;
            }
        }


        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (entities.TryGetComponent(component.Owner, out ISpriteComponent spriteComponent))
            {
                if (_spriteLayers.Count == 0)
                {
                    InitLayers(spriteComponent, component);
                }

                EnableLayers(spriteComponent, component);
            }
        }

        private void InitLayers(ISpriteComponent spriteComponent, AppearanceComponent component)
        {
            if (!component.TryGetData<ShowLayerData>(StorageMapVisuals.InitLayers, out var wrapper))
                return;

            _spriteLayers.AddRange(wrapper.QueuedEntities);

            foreach (var sprite in _spriteLayers)
            {
                spriteComponent.LayerMapReserveBlank(sprite);
                spriteComponent.LayerSetSprite(sprite, new SpriteSpecifier.Rsi(_rsiPath!, sprite));
                spriteComponent.LayerSetVisible(sprite, false);
            }
        }

        private void EnableLayers(ISpriteComponent spriteComponent, AppearanceComponent component)
        {
            if (!component.TryGetData<ShowLayerData>(StorageMapVisuals.LayerChanged, out var wrapper))
                return;


            foreach (var layerName in _spriteLayers)
            {
                var show = wrapper.QueuedEntities.Contains(layerName);
                spriteComponent.LayerSetVisible(layerName, show);
            }
        }
    }
}
