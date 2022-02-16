using System.Collections.Generic;
using Content.Client.Lathe.Components;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;
using static Content.Shared.Lathe.SharedLatheComponent;

namespace Content.Client.Lathe.UI
{
    public sealed class LatheBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables]
        private LatheMenu? _menu;
        [ViewVariables]
        private LatheQueueMenu? _queueMenu;

        public MaterialStorageComponent? Storage { get; private set; }
        public SharedLatheComponent? Lathe { get; private set; }
        public SharedLatheDatabaseComponent? Database { get; private set; }

        [ViewVariables]
        public Queue<LatheRecipePrototype> QueuedRecipes => _queuedRecipes;
        private readonly Queue<LatheRecipePrototype> _queuedRecipes = new();

        public LatheBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new LatheSyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();

            if (!_entMan.TryGetComponent(Owner.Owner, out MaterialStorageComponent? storage)
            ||  !_entMan.TryGetComponent(Owner.Owner, out SharedLatheComponent? lathe)
            ||  !_entMan.TryGetComponent(Owner.Owner, out SharedLatheDatabaseComponent? database)) return;

            Storage = storage;
            Lathe = lathe;
            Database = database;

            _menu = new LatheMenu(this);
            _queueMenu = new LatheQueueMenu(this);

            _menu.OnClose += Close;

            _menu.Populate();
            _menu.PopulateMaterials();

            _menu.QueueButton.OnPressed += (_) => { _queueMenu.OpenCentered(); };

            _menu.ServerConnectButton.OnPressed += (_) =>
            {
                SendMessage(new LatheServerSelectionMessage());
            };

            _menu.ServerSyncButton.OnPressed += (_) =>
            {
                SendMessage(new LatheServerSyncMessage());
            };

            storage.OnMaterialStorageChanged += _menu.PopulateDisabled;
            storage.OnMaterialStorageChanged += _menu.PopulateMaterials;

            _menu.OpenCentered();
        }

        public void Queue(LatheRecipePrototype recipe, int quantity = 1)
        {
            SendMessage(new LatheQueueRecipeMessage(recipe.ID, quantity));
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case LatheProducingRecipeMessage msg:
                    if (!_prototypeManager.TryIndex(msg.ID, out LatheRecipePrototype? recipe)) break;
                    _queueMenu?.SetInfo(recipe);
                    break;
                case LatheStoppedProducingRecipeMessage _:
                    _queueMenu?.ClearInfo();
                    break;
                case LatheFullQueueMessage msg:
                    _queuedRecipes.Clear();
                    foreach (var id in msg.Recipes)
                    {
                        if (!_prototypeManager.TryIndex(id, out LatheRecipePrototype? recipePrototype)) break;
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
