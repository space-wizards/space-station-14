using Content.Shared.Shuttles.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Shuttles
{
    public sealed class ThrusterVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out SpriteComponent? spriteComponent)) return;

            component.TryGetData(ThrusterVisualState.State, out bool state);

            switch (state)
            {
                case true:
                    spriteComponent.LayerSetVisible(ThrusterVisualLayers.ThrustOn, true);

                    if (component.TryGetData(ThrusterVisualState.Thrusting, out bool thrusting) && thrusting)
                    {
                        spriteComponent.LayerSetVisible(ThrusterVisualLayers.Thrusting, true);
                    }
                    else
                    {
                        spriteComponent.LayerSetVisible(ThrusterVisualLayers.Thrusting, false);
                    }

                    break;
                case false:
                    spriteComponent.LayerSetVisible(ThrusterVisualLayers.ThrustOn, false);
                    spriteComponent.LayerSetVisible(ThrusterVisualLayers.Thrusting, false);
                    break;
            }
        }
    }

    public enum ThrusterVisualLayers : byte
    {
        Base,
        ThrustOn,
        Thrusting
    }
}
