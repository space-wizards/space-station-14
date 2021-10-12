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
        [ViewVariables]
        public bool isFolded = false;

        public bool CanBeFolded => !Owner.IsInContainer();

        protected override void Initialize()
        {
            base.Initialize();

            // Update appearance
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                appearance.SetData("FoldedState", isFolded);
        }
    }
}
