using Content.Shared.GameObjects.Components.Storage;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Storage
{
    public sealed class StorageVisualizer2D : AppearanceVisualizer
    {
        private string _stateOpen;
        private string _stateClosed;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode("state_open", out var child))
            {
                _stateOpen = child.AsString();
            }

            if (node.TryGetNode("state_closed", out child))
            {
                _stateClosed = child.AsString();
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            component.TryGetData(StorageVisuals.Open, out bool open);
            sprite.LayerSetState(StorageVisualLayers.Door, open ? _stateOpen : _stateClosed);
        }
    }

    public enum StorageVisualLayers
    {
        Door
    }
}
