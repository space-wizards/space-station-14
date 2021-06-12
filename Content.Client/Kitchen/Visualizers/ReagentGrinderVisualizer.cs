using Robust.Client.GameObjects;
using static Content.Shared.Kitchen.Components.SharedReagentGrinderComponent;

namespace Content.Client.Kitchen.Visualizers
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
