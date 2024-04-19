using Robust.Client.GameObjects;
using Content.Shared.Fax.Components;
using Content.Shared.Fax;

namespace Content.Client.Fax.System
{
    /// <summary>
    /// Visualizer for the fax machine which displays the correct sprite based on the inserted entity.
    /// </summary>
    public sealed class FaxVisualsSystem : EntitySystem
    {

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

            if (_appearance.TryGetData(uid, FaxMachineVisuals.VisualState, out bool _))
            {
                args.Sprite.LayerSetState(FaxMachineVisuals.VisualState, component.InsertingState);
            }
        }
    }
}
