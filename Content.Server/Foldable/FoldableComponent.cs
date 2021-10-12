#nullable enable

using Content.Shared.Foldable;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Foldable
{

    /// <inheritdoc cref="SharedFoldableComponent"/>
    [RegisterComponent]
    [ComponentReference(typeof(SharedFoldableComponent))]
    public class FoldableComponent : SharedFoldableComponent
    {
        private bool _isFolded = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsFolded
        {
            get => _isFolded;
            set
            {
                // Update visuals only if the value has changed
                if (_isFolded != value && Owner.TryGetComponent(out AppearanceComponent? appearance))
                    appearance.SetData(FoldableVisuals.FoldedState, value);
                _isFolded = value;
            }
        }

        public bool CanBeFolded => !Owner.IsInContainer();

        protected override void Initialize()
        {
            base.Initialize();

            // Update appearance
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                appearance.SetData(FoldableVisuals.FoldedState, _isFolded);
        }
    }
}
