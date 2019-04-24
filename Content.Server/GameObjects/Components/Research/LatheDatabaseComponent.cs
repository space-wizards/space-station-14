using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;

namespace Content.Server.GameObjects.Components.Research
{
    public class LatheDatabaseComponent : SharedLatheDatabaseComponent
    {
        public override bool Clear()
        {
            if (!base.Clear()) return false;
            SendNetworkMessage(new LatheDatabaseClearMessage());
            return true;
        }

        public override bool AddRecipe(LatheRecipePrototype recipe)
        {
            if (!base.AddRecipe(recipe)) return false;
            SendNetworkMessage(new LatheDatabaseRecipeAddMessage(recipe.ID));
            return true;
        }

        public override bool RemoveRecipe(LatheRecipePrototype recipe)
        {
            if (!base.RemoveRecipe(recipe)) return false;
            SendNetworkMessage(new LatheDatabaseRecipeRemoveMessage(recipe.ID));
            return true;
        }
    }
}
