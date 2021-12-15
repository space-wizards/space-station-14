using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Visualizer
{
    /// <summary>
    /// General purpose appearance visualizer used to toggle back and forth between two states
    /// </summary>
    [UsedImplicitly]
    public class RollerbedVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent appearance)
        {
            base.OnChangeData(appearance);

            var entManager = IoCManager.Resolve<IEntityManager>();

            if (!entManager.TryGetComponent(appearance.Owner, out SpriteComponent? sprite)) return;

            if (appearance.TryGetData("StrapState", out bool strapped) && strapped)
            {
                sprite.LayerSetState(0, "rollerbed_buckled");
            }
            else if (appearance.TryGetData("FoldedState", out bool folded) && folded)
            {
                sprite.LayerSetState(0, "rollerbed_folded");
            }
            else
            {
                sprite.LayerSetState(0, "rollerbed");
            }
        }
    }
}
