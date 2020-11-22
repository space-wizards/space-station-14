using System.Collections.Generic;
using Content.Client.Research;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Research
{
    public class LatheBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables]
        private LatheMenu _menu;
        [ViewVariables]
        private LatheQueueMenu _queueMenu;

        public MaterialStorageComponent Storage { get; private set; }
        public SharedLatheComponent Lathe { get; private set; }
        public SharedLatheDatabaseComponent Database { get; private set; }

        [ViewVariables]
        public Queue<LatheRecipePrototype> QueuedRecipes => _queuedRecipes;
        private readonly Queue<LatheRecipePrototype> _queuedRecipes = new Queue<LatheRecipePrototype>();

        public LatheBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new SharedLatheComponent.LatheSyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();

            if (!Owner.Owner.TryGetComponent(out MaterialStorageComponent storage)
            ||  !Owner.Owner.TryGetComponent(out SharedLatheComponent lathe)
            ||  !Owner.Owner.TryGetComponent(out SharedLatheDatabaseComponent database)) return;



            Storage = storage;
            Lathe = lathe;
            Database = database;

            _menu = new LatheMenu(this);
            _queueMenu = new LatheQueueMenu { Owner = this };

            _menu.OnClose += Close;

            _menu.Populate();
            _menu.PopulateMaterials();

            _menu.QueueButton.OnPressed += (args) => { _queueMenu.OpenCentered(); };

            _menu.ServerConnectButton.OnPressed += (args) =>
            {
                SendMessage(new SharedLatheComponent.LatheServerSelectionMessage());
            };

            _menu.ServerSyncButton.OnPressed += (args) =>
            {
                SendMessage(new SharedLatheComponent.LatheServerSyncMessage());
            };

            storage.OnMaterialStorageChanged += _menu.PopulateDisabled;
            storage.OnMaterialStorageChanged += _menu.PopulateMaterials;

            _menu.OpenCentered();
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
                    if (!_prototypeManager.TryIndex(msg.ID, out LatheRecipePrototype recipe)) break;
                    _queueMenu?.SetInfo(recipe);
                    break;
                case SharedLatheComponent.LatheStoppedProducingRecipeMessage _:
                    _queueMenu?.ClearInfo();
                    break;
                case SharedLatheComponent.LatheFullQueueMessage msg:
                    _queuedRecipes.Clear();
                    foreach (var id in msg.Recipes)
                    {
                        if (!_prototypeManager.TryIndex(id, out LatheRecipePrototype recipePrototype)) break;
                        _queuedRecipes.Enqueue(recipePrototype);
                    }
                    _queueMenu?.PopulateList();
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _menu?.Dispose();
            _queueMenu?.Dispose();
        }
    }
}
