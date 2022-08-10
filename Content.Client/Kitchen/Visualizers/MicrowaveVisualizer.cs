using Content.Client.Kitchen.Components;
using Content.Client.Kitchen.EntitySystems;
using Content.Shared.Kitchen.Components;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Client.Kitchen.Visualizers
{
    [UsedImplicitly]
    public sealed class MicrowaveVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var entMan = IoCManager.Resolve<IEntityManager>();
            var sprite = entMan.GetComponent<ISpriteComponent>(component.Owner);

            var microwaveComponent = entMan.GetComponentOrNull<MicrowaveComponent>(component.Owner);

            if (!component.TryGetData(PowerDeviceVisuals.VisualState, out MicrowaveVisualState state))
            {
                state = MicrowaveVisualState.Idle;
            }
            // The only reason we get the entity system so late is so that tests don't fail... Amazing, huh?
            switch (state)
            {
                case MicrowaveVisualState.Broken:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mwb");
                    if(microwaveComponent != null)
                        EntitySystem.Get<MicrowaveSystem>().StopSoundLoop(microwaveComponent);
                    break;

                case MicrowaveVisualState.Idle:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_unlit");
                    if(microwaveComponent != null)
                        EntitySystem.Get<MicrowaveSystem>().StopSoundLoop(microwaveComponent);
                    break;

                case MicrowaveVisualState.Cooking:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_running_unlit");
                    if(microwaveComponent != null)
                        EntitySystem.Get<MicrowaveSystem>().StartSoundLoop(microwaveComponent);
                    break;

                default:
                    Logger.Debug($"Something terrible happened in {this}");
                    break;

            }

            var glowingPartsVisible = !(component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) && !powered);
            sprite.LayerSetVisible(MicrowaveVisualizerLayers.BaseUnlit, glowingPartsVisible);
        }

        private enum MicrowaveVisualizerLayers : byte
        {
            Base,
            BaseUnlit
        }
    }


}
