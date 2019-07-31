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
#pragma warning disable CS0649
        [Dependency]
        private IPrototypeManager _prototypeManager;
#pragma warning restore

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
