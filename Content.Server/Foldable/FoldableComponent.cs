#nullable enable

using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Foldable
{

    /// <summary>
    /// Used to create "foldable structures" that you can pickup like an item when folded. Used for rollerbeds and wheelchairs
    /// </summary>
    [RegisterComponent]
    public class FoldableComponent : Component
    {
        public override string Name => "Foldable";

        [ViewVariables]
        public bool IsFolded = false;

        public bool CanBeFolded = true;
    }
}
