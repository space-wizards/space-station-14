using Content.Shared.Lathe;
using Content.Shared.Research.Components;
using JetBrains.Annotations;

namespace Content.Client.Lathe.UI
{
    [UsedImplicitly]
    public sealed class LatheBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private LatheMenu? _menu;
        public LatheBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new LatheMenu(this);
            _menu.OnClose += Close;


            _menu.OnServerListButtonPressed += _ =>
            {
                SendMessage(new ConsoleServerSelectionMessage());
            };

            _menu.RecipeQueueAction += (recipe, amount) =>
            {
                SendMessage(new LatheQueueRecipeMessage(recipe, amount));
            };

            _menu.OpenCenteredRight();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case LatheUpdateState msg:
                    if (_menu != null)
                        _menu.Recipes = msg.Recipes;
                    _menu?.PopulateRecipes();
                    _menu?.UpdateCategories();
                    _menu?.PopulateQueueList(msg.Queue);
                    _menu?.SetQueueInfo(msg.CurrentlyProducing);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;
            _menu?.Dispose();
        }
    }
}
