using Content.Client.GameObjects.Components.Sound;
using Content.Shared.GameObjects.Components.Morgue;
using Content.Shared.GameObjects.Components.Sound;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Audio;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Storage
{
    public sealed class CrematoriumVisualizer : AppearanceVisualizer
    {
        private string _stateOpen;
        private string _stateClosed;

        private string _lightContents;
        private string _lightBurning;

        private LoopingSoundComponent _loopingSoundComponent;


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

            _loopingSoundComponent ??= component.Owner.GetComponent<LoopingSoundComponent>();

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            sprite.LayerSetState(
                CrematoriumVisualLayers.Base,
                component.GetData<bool>(MorgueVisuals.Open)
                    ? _stateOpen
                    : _stateClosed
            );

            var lightState = "";
            if (component.TryGetData(MorgueVisuals.HasContents,  out bool hasContents) && hasContents) lightState = _lightContents;
            if (component.TryGetData(CrematoriumVisuals.Burning, out bool isBurning)   && isBurning)   lightState = _lightBurning;

            /* TODO: get a nice fire loop
            if (isBurning)
            {
                var scheduledSound = new ScheduledSound();
                scheduledSound.Filename = "/Audio/Effects/fireloop.ogg";
                scheduledSound.AudioParams = AudioParams.Default.WithLoop(true);
                _loopingSoundComponent.StopAllSounds();
                _loopingSoundComponent.AddScheduledSound(scheduledSound);
            }
            else
            {
                _loopingSoundComponent.StopAllSounds();
            }*/

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

    public enum CrematoriumVisualLayers
    {
        Base,
        Light,
    }
}
