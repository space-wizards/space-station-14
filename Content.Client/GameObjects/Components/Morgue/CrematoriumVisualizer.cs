#nullable enable
using Content.Shared.GameObjects.Components.Morgue;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Storage
{
    public sealed class CrematoriumVisualizer : AppearanceVisualizer
    {
        private string _stateOpen = "";
        private string _stateClosed = "";

        private string _lightContents = "";
        private string _lightBurning = "";

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

            if (node.TryGetNode("light_contents", out child))
            {
                _lightContents = child.AsString();
            }
            if (node.TryGetNode("light_burning", out child))
            {
                _lightBurning = child.AsString();
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite)) return;

            if (component.TryGetData(MorgueVisuals.Open, out bool open))
            {
                sprite.LayerSetState(CrematoriumVisualLayers.Base, open ? _stateOpen : _stateClosed);
            }
            else
            {
                sprite.LayerSetState(CrematoriumVisualLayers.Base, _stateClosed);
            }

            var lightState = "";
            if (component.TryGetData(MorgueVisuals.HasContents,  out bool hasContents) && hasContents) lightState = _lightContents;
            if (component.TryGetData(CrematoriumVisuals.Burning, out bool isBurning)   && isBurning)   lightState = _lightBurning;

            if (!string.IsNullOrEmpty(lightState))
            {
                sprite.LayerSetState(CrematoriumVisualLayers.Light, lightState);
                sprite.LayerSetVisible(CrematoriumVisualLayers.Light, true);
            }
            else
            {
                sprite.LayerSetVisible(CrematoriumVisualLayers.Light, false);
            }
        }
    }

    public enum CrematoriumVisualLayers : byte
    {
        Base,
        Light,
    }
}
