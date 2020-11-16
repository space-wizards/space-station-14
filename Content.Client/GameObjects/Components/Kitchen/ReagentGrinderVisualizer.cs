using Content.Shared.GameObjects.Components.Power;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using static Content.Shared.Kitchen.SharedReagentGrinderComponent;

namespace Content.Client.GameObjects.Components.Kitchen
{
    public class ReagentGrinderVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(PowerDeviceVisuals.VisualState, out ReagentGrinderVisualState state))
            {
                state = ReagentGrinderVisualState.NoBeaker;
            }
            switch(state)
            {
                case ReagentGrinderVisualState.NoBeaker:
                    sprite.LayerSetState(0, "juicer0");
                    break;
                case ReagentGrinderVisualState.BeakerAttached:
                    sprite.LayerSetState(0, "juicer1");
                    break;
            }
        }
    }
}
