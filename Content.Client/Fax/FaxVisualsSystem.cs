using Robust.Client.GameObjects;
using Content.Shared.Fax;
using Robust.Shared.Prototypes;

namespace Content.Client.Paint
{
    public sealed class FaxVisualsSystem : EntitySystem
    {
        /// <summary>
        /// Visualizer for Paint which applies a shader and colors the entity.
        /// </summary>

        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FaxMachineComponent, AppearanceChangeEvent>(OnAppearanceChanged);
        }

        private void OnAppearanceChanged(EntityUid uid, FaxMachineComponent component, ref AppearanceChangeEvent args)
        {

            if (args.Sprite == null)
                return;

            if (_appearance.TryGetData<bool>(uid, FaxMachineVisuals.VisualState, out bool inserted))
            {
                args.Sprite.LayerSetState(FaxMachineVisuals.VisualState, component.InsertingState);
            }
        }
    }
}
