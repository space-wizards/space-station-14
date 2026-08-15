using Content.Shared.Lathe;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

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

            if (EntMan.TryGetComponent<LatheComponent>(Owner, out var latheComp))
            {
                UpdateProductionQueue(latheComp.CurrentRecipe, latheComp.Queue);
            }
            _menu.RefreshRecipes();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);

            if (message is LatheRefreshRecipesMessage)
            {
                _menu?.RefreshRecipes();
            }
        }

        /// <summary>
        /// Update the production queue portion of the UI
        /// </summary>
        /// <param name="current">Currently-being-produced item, if any</param>
        /// <param name="remainder">Iterator for remaining items in production queue</param>
        public void UpdateProductionQueue(ProtoId<LatheRecipePrototype>? current, IReadOnlyCollection<LatheRecipeBatch> remainder)
        {
            _menu?.PopulateQueueList(remainder);
            _menu?.SetQueueInfo(current);
        }

        /// <summary>
        /// Update UI after stored material quantities has changed
        /// </summary>
        public void UpdateMaterialAmounts()
        {
            _menu?.UpdateMaterialAmounts();
        }
    }
}
