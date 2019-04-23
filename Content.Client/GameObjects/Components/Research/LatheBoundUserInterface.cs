using System.Collections.Generic;
using Content.Client.Research;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Research
{
    public class LatheBoundUserInterface : BoundUserInterface
    {
#pragma warning disable CS0649
        [Dependency]
        private IDisplayManager _displayManager;
        [Dependency]
        private IPrototypeManager _prototypeManager;
#pragma warning restore
        [ViewVariables]
        private LatheMenu menu;
        [ViewVariables]
        private LatheQueueMenu queueMenu;

        public MaterialStorageComponent Storage;
        public SharedLatheComponent Lathe;

        [ViewVariables]
        public Queue<LatheRecipePrototype> QueuedRecipes => _queuedRecipes;
        private Queue<LatheRecipePrototype> _queuedRecipes = new Queue<LatheRecipePrototype>();

        public LatheBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new SharedLatheComponent.LatheSyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();
            IoCManager.InjectDependencies(this);

            if (!Owner.Owner.TryGetComponent(out Storage)) return;
            if (!Owner.Owner.TryGetComponent(out Lathe)) return;

            menu = new LatheMenu(_displayManager) {Owner = this};
            queueMenu = new LatheQueueMenu(_displayManager) { Owner = this };
            menu.AddToScreen();
            menu.PopulateRecipes();
            queueMenu.AddToScreen();

            menu.QueueButton.OnPressed += (args) => { queueMenu.OpenCentered(); };

            if (!Owner.Owner.TryGetComponent(out MaterialStorageComponent storage)) return;
            storage.OnMaterialStorageChanged += menu.PopulateDisabled;
            storage.OnMaterialStorageChanged += menu.PopulateMaterials;

            menu.OpenCentered();
        }

        public void Queue(LatheRecipePrototype recipe, int quantity = 1)
        {
            SendMessage(new SharedLatheComponent.LatheQueueRecipeMessage(recipe.ID, quantity));
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case SharedLatheComponent.LatheProducingRecipeMessage msg:
                    _prototypeManager.TryIndex(msg.ID, out LatheRecipePrototype recipe);
                    if (recipe != null)
                    {
                        queueMenu.SetInfo(recipe);
                    }
                    break;
                case SharedLatheComponent.LatheStoppedProducingRecipeMessage msg:
                    queueMenu.ClearInfo();
                    break;
                case SharedLatheComponent.LatheFullQueueMessage msg:
                    _queuedRecipes.Clear();
                    foreach (var id in msg.Recipes)
                    {
                        _prototypeManager.TryIndex(id, out LatheRecipePrototype recipePrototype);
                        if (recipePrototype != null)
                            _queuedRecipes.Enqueue(recipePrototype);
                    }
                    queueMenu.PopulateList();
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            menu?.Dispose();
            queueMenu?.Dispose();
        }
    }
}
