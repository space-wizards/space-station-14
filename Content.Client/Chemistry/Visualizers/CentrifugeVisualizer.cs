using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.Chemistry.SharedCentrifuge;

namespace Content.Client.Chemistry.Visualizers
{
    public sealed class CentrifugeVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")] //sooorry
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<SpriteComponent>(component.Owner);

            component.TryGetData(CentrifugeVisualState.BeakerAttached, out bool hasBeaker);
            component.TryGetData(CentrifugeVisualState.OutputAttached, out bool hasOutput);

            if (hasBeaker && hasOutput)
                sprite.LayerSetState(0, $"centrifuge3");
            else if (hasBeaker)
                sprite.LayerSetState(0, $"centrifuge1");
            else if (hasOutput)
                sprite.LayerSetState(0, $"centrifuge2");
            else
                sprite.LayerSetState(0, $"centrifuge0");
        }
    }
}
