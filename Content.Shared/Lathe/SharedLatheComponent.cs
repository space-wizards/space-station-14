using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lathe
{
    [NetworkedComponent()]
    [Virtual]
    public class SharedLatheComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        public bool CanProduce(LatheRecipePrototype recipe, int quantity = 1)
        {
            /*
            if (!_entMan.TryGetComponent(Owner, out MaterialStorageComponent? storage)
            ||  !_entMan.TryGetComponent(Owner, out SharedLatheDatabaseComponent? database)) return false;

            if (!database.Contains(recipe)) return false;

            foreach (var (material, amount) in recipe.RequiredMaterials)
            {
                if (storage[material] < (amount * quantity)) return false;
            }
            */
            return true;
        }

        public bool CanProduce(string id, int quantity = 1)
        {
            return PrototypeManager.TryIndex(id, out LatheRecipePrototype? recipe) && CanProduce(recipe, quantity);
        }


        [DataField("idleState", required: true)]
        public string IdleState = default!;

        [DataField("runningState", required: true)]
        public string RunningState = default!;

        [ViewVariables]
        [DataField("ignoreColor")]
        public bool IgnoreColor;
    }
}
