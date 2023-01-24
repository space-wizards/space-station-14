using Robust.Client.GameObjects;
using Content.Shared.Kitchen;

namespace Content.Client.Kitchen.Visualizers
{
    public sealed class ReagentGrinderVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<SpriteComponent>(component.Owner);
            component.TryGetData(ReagentGrinderVisualState.BeakerAttached, out bool hasBeaker);
            sprite.LayerSetState(0, $"juicer{(hasBeaker ? "1" : "0")}");
        }
    }
}
