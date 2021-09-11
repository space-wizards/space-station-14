using Content.Shared.Juke;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Juke
{
    [UsedImplicitly]
    public sealed class MidiJukeVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(PowerDeviceVisuals.VisualState, out MidiJukeVisualState state))
            {
                state = MidiJukeVisualState.Base;
            }

            switch (state)
            {
                case MidiJukeVisualState.Base:
                    sprite.LayerSetState(MidiJukeVisualizerLayers.Base, "icon");
                    break;
                case MidiJukeVisualState.Broken: //TODO: broken sprite
                    break;
            }

            var glow = component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) && powered;
            sprite.LayerSetVisible(MidiJukeVisualizerLayers.BaseUnlit, glow);
        }

        private enum MidiJukeVisualizerLayers : byte
        {
            Base,
            BaseUnlit
        }
    }
}
