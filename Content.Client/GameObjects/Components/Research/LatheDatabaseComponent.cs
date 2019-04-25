using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.Components.Research
{
    public class LatheDatabaseComponent : SharedLatheDatabaseComponent
    {
#pragma warning disable CS0649
        [Dependency]
        private IPrototypeManager _prototypeManager;
#pragma warning restore

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);
            switch (message)
            {
                case LatheDatabaseSyncMessage msg:
                    if (Static) return;
                    Clear();
                    foreach (var recipeID in msg.Recipes)
                    {
                        if(!_prototypeManager.TryIndex(recipeID, out LatheRecipePrototype recipe)) continue;
                        AddRecipe(recipe);
                    }
                    break;

                case LatheDatabaseRecipeAddMessage msg:
                    if (Static) return;
                    if(!_prototypeManager.TryIndex(msg.Recipe, out LatheRecipePrototype addedRecipe)) break;
                    AddRecipe(addedRecipe);
                    break;

                case LatheDatabaseRecipeRemoveMessage msg:
                    if (Static) return;
                    if(!_prototypeManager.TryIndex(msg.Recipe, out LatheRecipePrototype removedRecipe)) break;
                    RemoveRecipe(removedRecipe);
                    break;

                case LatheDatabaseClearMessage msg:
                    if (Static) return;
                    Clear();
                    break;
            }
        }
    }
}
