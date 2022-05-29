using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Construction
{
    [UsedImplicitly]
    public sealed class MachineFrameVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<int>(MachineFrameVisuals.State, out var data))
            {
                var sprite = IoCManager.Resolve<IEntityManager>().GetComponent<ISpriteComponent>(component.Owner);

                sprite.LayerSetState(0, $"box_{data}");
            }
        }
    }
}
