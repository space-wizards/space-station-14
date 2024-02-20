using System.Linq;
using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;
using Content.Shared.Clothing;
using Content.Shared.Fax;
using Content.Shared.Hands;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Storage.Components;

namespace Content.Client.Paint
{
    public sealed class FaxVisualsSystem : EntitySystem
    {
        /// <summary>
        /// Visualizer for Paint which applies a shader and colors the entity.
        /// </summary>

        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;

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
