#nullable enable

using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Foldable
{

    /// <inheritdoc cref="SharedFoldableComponent"/>
    [RegisterComponent]
    [ComponentReference(typeof(FoldableComponent))]
    public class FoldableComponent : Component
    {
        public override string Name => "Foldable";

        [ViewVariables]
        public bool isFolded = false;

        public bool CanBeFolded => !Owner.IsInContainer();
    }
}
