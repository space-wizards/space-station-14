using Content.Shared.GameObjects.Components.PDA;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.PDA
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public class PDAVisualizer : AppearanceVisualizer
    {
        /// <summary>
        /// The base PDA sprite state, eg. "pda", "pda-clown"
        /// </summary>
        private string _state;

        private enum PDAVisualLayers : byte
        {
            Base,
            Flashlight,
            IDLight
        }

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            if (node.TryGetNode("state", out var child))
            {
                _state = child.AsString();
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapSet(PDAVisualLayers.Base, sprite.AddLayerState(_state));
            sprite.LayerMapSet(PDAVisualLayers.Flashlight, sprite.AddLayerState("light_overlay"));
            sprite.LayerSetShader(PDAVisualLayers.Flashlight, "unshaded");
            sprite.LayerMapSet(PDAVisualLayers.IDLight, sprite.AddLayerState("id_overlay"));
            sprite.LayerSetShader(PDAVisualLayers.IDLight, "unshaded");
        }


        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            sprite.LayerSetVisible(PDAVisualLayers.Flashlight, false);
            if (component.TryGetData(PDAVisuals.FlashlightLit, out bool isScreenLit))
            {
                sprite.LayerSetVisible(PDAVisualLayers.Flashlight, isScreenLit);
            }

            if (component.TryGetData(PDAVisuals.IDCardInserted, out bool isCardInserted))
            {
                sprite.LayerSetVisible(PDAVisualLayers.IDLight, isCardInserted);
            }

        }


    }
}
