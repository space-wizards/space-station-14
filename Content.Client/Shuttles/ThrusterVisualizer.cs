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
                    spriteComponent.LayerSetVisible(ThrusterVisualLayers.Thrust, true);
                    break;
                case false:
                    spriteComponent.LayerSetVisible(ThrusterVisualLayers.Thrust, false);
                    break;
            }
        }
    }

    public enum ThrusterVisualLayers : byte
    {
        Base = 0,
        Thrust = 1,
    }
}
