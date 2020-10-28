#nullable enable
using Content.Shared.GameObjects.Components.Morgue;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Storage
{
    public sealed class MorgueVisualizer : AppearanceVisualizer
    {
        private string _stateOpen = "";
        private string _stateClosed = "";

        private string _lightContents = "";
        private string _lightMob = "";
        private string _lightSoul = "";

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
            if (node.TryGetNode("light_mob", out child))
            {
                _lightMob = child.AsString();
            }
            if (node.TryGetNode("light_soul", out child))
            {
                _lightSoul = child.AsString();
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite)) return;

            sprite.LayerSetState(
                MorgueVisualLayers.Base,
                component.GetData<bool>(MorgueVisuals.Open)
                    ? _stateOpen
                    : _stateClosed
            );

            var lightState = "";
            if (component.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents) lightState = _lightContents;
            if (component.TryGetData(MorgueVisuals.HasMob,      out bool hasMob)      && hasMob)      lightState = _lightMob;
            if (component.TryGetData(MorgueVisuals.HasSoul,     out bool hasSoul)     && hasSoul)     lightState = _lightSoul;

            if (!string.IsNullOrEmpty(lightState))
            {
                sprite.LayerSetState(MorgueVisualLayers.Light, lightState);
                sprite.LayerSetVisible(MorgueVisualLayers.Light, true);
            }
            else
            {
                sprite.LayerSetVisible(MorgueVisualLayers.Light, false);
            }
        }
    }

    public enum MorgueVisualLayers
    {
        Base,
        Light,
    }
}
