using System;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    public class ProtolatheDatabaseComponent : SharedProtolatheDatabaseComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        /// <summary>
        ///     Invoked when the database gets updated.
        /// </summary>
        public event Action OnDatabaseUpdated;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not ProtolatheDatabaseState state) return;
            Clear();
            foreach (var ID in state.Recipes)
            {
                if(!_prototypeManager.TryIndex(ID, out LatheRecipePrototype recipe)) continue;
                AddRecipe(recipe);
            }

            OnDatabaseUpdated?.Invoke();
        }
    }
}
