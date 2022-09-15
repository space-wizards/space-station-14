using Content.Shared.Buckle.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Visualizer
{
    [UsedImplicitly]
    public sealed class RollerbedVisualizer : AppearanceVisualizer
    {
        [DataField("key")]
        private string _key = default!;

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent appearance)
        {
            base.OnChangeData(appearance);

            var entManager = IoCManager.Resolve<IEntityManager>();

            if (!entManager.TryGetComponent(appearance.Owner, out SpriteComponent? sprite)) return;

            if (appearance.TryGetData(StrapVisuals.State, out bool strapped) && strapped)
            {
                sprite.LayerSetState(0, $"{_key}_buckled");
            }
        }
    }
}
