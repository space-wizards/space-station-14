using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Construction
{
    [UsedImplicitly]
    public class MachineFrameVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<int>(MachineFrameVisuals.State, out var data))
            {
                var sprite = component.Owner.GetComponent<ISpriteComponent>();

                sprite.LayerSetState(0, $"box_{data}");
            }
        }
    }
}
