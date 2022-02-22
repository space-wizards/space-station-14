using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.Kitchen.Components.SharedReagentGrinderComponent;

namespace Content.Client.Kitchen.Visualizers
{
    public sealed class ReagentGrinderVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);
            component.TryGetData(ReagentGrinderVisualState.BeakerAttached, out bool hasBeaker);
            sprite.LayerSetState(0, $"juicer{(hasBeaker ? "1" : "0")}");
        }
    }
}
