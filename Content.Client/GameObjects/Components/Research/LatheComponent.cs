using System.Collections.Generic;
using Content.Client.Research;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.Components.Research
{
    public class LatheComponent : SharedLatheComponent
    {
        [Dependency]
        private IDisplayManager _displayManager;
        [Dependency]
        private IPrototypeManager _prototypeManager;
        private LatheMenu menu;
        private LatheQueueMenu queueMenu;

        public Queue<LatheRecipePrototype> QueuedRecipes => _queuedRecipes;
        private Queue<LatheRecipePrototype> _queuedRecipes = new Queue<LatheRecipePrototype>();

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);

            menu = new LatheMenu(_displayManager) {Owner = this};
            queueMenu = new LatheQueueMenu(_displayManager) { Owner = this };
            menu.AddToScreen();
            menu.PopulateRecipes();
            queueMenu.AddToScreen();

            menu.QueueButton.OnPressed += (args) => { queueMenu.OpenCentered(); };

            Owner.TryGetComponent(out MaterialStorageComponent storage);
            if (storage != null)
            {
                storage.OnMaterialStorageChanged += menu.PopulateDisabled;
                storage.OnMaterialStorageChanged += menu.PopulateMaterials;
            }

        }

        public void Queue(LatheRecipePrototype recipe, int quantity = 1)
        {
            SendNetworkMessage(new LatheQueueRecipeMessage(recipe.ID, quantity));
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            switch (message)
            {
                case LatheMenuOpenMessage msg:
                    menu.OpenCentered();
                    break;
                case LatheProducingRecipeMessage msg:
                    _prototypeManager.TryIndex(msg.ID, out LatheRecipePrototype recipe);
                    if (recipe != null)
                    {
                        queueMenu.SetInfo(recipe);
                    }
                    break;
                case LatheStoppedProducingRecipeMessage msg:
                    queueMenu.ClearInfo();
                    break;
                case LatheFullQueueMessage msg:
                    _queuedRecipes.Clear();
                    foreach (var id in msg.Recipes)
                    {
                        Logger.Info($"{id} {_prototypeManager == null}");
                        _prototypeManager.TryIndex(id, out LatheRecipePrototype recipePrototype);
                        if (recipePrototype != null)
                            _queuedRecipes.Enqueue(recipePrototype);
                    }
                    queueMenu.PopulateList();
                    break;
            }
        }

        public override void OnRemove()
        {
            menu?.Dispose();
        }
    }
}
