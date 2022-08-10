using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.Lathe.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    public sealed class LatheDatabaseComponent : SharedLatheDatabaseComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not LatheDatabaseState state) return;

            Clear();

            foreach (var id in state.Recipes)
            {
                if (!_prototypeManager.TryIndex(id, out LatheRecipePrototype? recipe)) continue;
                AddRecipe(recipe);
            }
        }
    }
}
