using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    public class ProtolatheDatabaseComponent : SharedProtolatheDatabaseComponent
    {
        public override string Name => "ProtolatheDatabase";

        public override ComponentState GetComponentState()
        {
            return new ProtolatheDatabaseState(GetRecipeIdList());
        }

        /// <summary>
        ///     Adds unlocked recipes from technologies to the database.
        /// </summary>
        public void Sync()
        {
            if (!Owner.TryGetComponent(out TechnologyDatabaseComponent database)) return;

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var technology in database.Technologies)
            {
                foreach (var id in technology.UnlockedRecipes)
                {
                    var recipe = (LatheRecipePrototype)prototypeManager.Index(typeof(LatheRecipePrototype), id);
                    UnlockRecipe(recipe);
                }
            }

            Dirty();
        }

        /// <summary>
        ///     Unlocks a recipe but only if it's one of the allowed recipes on this protolathe.
        /// </summary>
        /// <param name="recipe">The recipe</param>
        /// <returns>Whether it could add it or not.</returns>
        public bool UnlockRecipe(LatheRecipePrototype recipe)
        {
            if (!ProtolatheRecipes.Contains(recipe)) return false;

            AddRecipe(recipe);

            return true;
        }
    }
}
