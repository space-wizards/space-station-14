using Content.Shared.Lathe;
using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

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

            _menu = this.CreateWindowCenteredRight<LatheMenu>();
            _menu.SetEntity(Owner);

            _menu.OnServerListButtonPressed += _ =>
            {
                SendMessage(new ConsoleServerSelectionMessage());
            };

            _menu.RecipeQueueAction += (recipe, amount) =>
            {
                SendMessage(new LatheQueueRecipeMessage(recipe, amount));
            };
            _menu.QueueDeleteAction += index => SendMessage(new LatheDeleteRequestMessage(index));
            _menu.QueueMoveUpAction += index => SendMessage(new LatheMoveRequestMessage(index, -1));
            _menu.QueueMoveDownAction += index => SendMessage(new LatheMoveRequestMessage(index, 1));
            _menu.DeleteFabricatingAction += () => SendMessage(new LatheAbortFabricationMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            switch (state)
            {
                case LatheUpdateState msg:
                    if ((msg.UpdateFlags & LatheUpdateState.UpdateWhat.Recipes) != 0)
                    {
                        if (_menu != null && msg.Recipes != null)
                            _menu.Recipes = msg.Recipes;
                        _menu?.PopulateRecipes();
                        _menu?.UpdateCategories();
                    }

                    if ((msg.UpdateFlags & LatheUpdateState.UpdateWhat.ProductionQueue) != 0)
                    {
                        if (msg.Queue != null)
                        {
                            _menu?.PopulateQueueList(msg.Queue);
                        }
                        _menu?.SetQueueInfo(msg.CurrentlyProducing);
                    }

                    if ((msg.UpdateFlags & LatheUpdateState.UpdateWhat.Materials) != 0)
                    {
                        _menu?.UpdateCanProduce();
                    }
                    break;
            }
        }
    }
}
