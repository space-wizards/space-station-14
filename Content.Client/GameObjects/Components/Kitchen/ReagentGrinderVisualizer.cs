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
            component.TryGetData(ReagentGrinderVisualState.BeakerAttached, out bool hasBeaker);
            sprite.LayerSetState(0, $"juicer{(hasBeaker ? "1" : "0")}");
        }
    }
}
