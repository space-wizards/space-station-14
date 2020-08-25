using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    public class LatheDatabaseComponent : SharedLatheDatabaseComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (!(curState is LatheDatabaseState state)) return;
            Clear();
            foreach (var ID in state.Recipes)
            {
                if(!_prototypeManager.TryIndex(ID, out LatheRecipePrototype recipe)) continue;
                AddRecipe(recipe);
            }
        }
    }
}
