using Content.Shared.Lathe;
using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Lathe.UI
{
    [UsedImplicitly]
    public sealed class LatheBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private LatheMenu? _menu;
        [ViewVariables] private LatheQueueMenu? _queueMenu;

        public EntityUid Lathe;

        public LatheBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
            Lathe = owner.Owner;
        }

        protected override void Open()
        {
            base.Open();

            _menu = new LatheMenu(this);
            _queueMenu = new LatheQueueMenu();

            _menu.OnClose += Close;

            _menu.OnQueueButtonPressed += _ =>
            {
                if (_queueMenu.IsOpen)
                    _queueMenu.Close();
                else
                    _queueMenu.OpenCenteredLeft();
            };
            _menu.OnServerListButtonPressed += _ =>
            {
                SendMessage(new ConsoleServerSelectionMessage());
            };
            _menu.RecipeQueueAction += (recipe, amount) =>
            {
                SendMessage(new LatheQueueRecipeMessage(recipe, amount));
            };

            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case LatheUpdateState msg:
                    if (_menu != null)
                        _menu.Recipes = msg.Recipes;
                    _menu?.PopulateRecipes(Lathe);
                    _menu?.PopulateMaterials(Lathe);
                    _queueMenu?.PopulateList(msg.Queue);
                    _queueMenu?.SetInfo(msg.CurrentlyProducing);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;
            _menu?.Dispose();
            _queueMenu?.Dispose();
        }
    }
}
