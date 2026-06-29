using Content.Shared.Lathe;
using Content.Shared.Lathe.Components;
using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Lathe.UI;

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
            SendPredictedMessage(new ConsoleServerSelectionMessage());
        };

        _menu.RecipeQueueAction += (recipe, amount) =>
        {
            SendPredictedMessage(new LatheQueueRecipeMessage(recipe, amount));
        };
        _menu.QueueDeleteAction += index => SendPredictedMessage(new LatheDeleteRequestMessage(index));
        _menu.QueueMoveUpAction += index => SendPredictedMessage(new LatheMoveRequestMessage(index, -1));
        _menu.QueueMoveDownAction += index => SendPredictedMessage(new LatheMoveRequestMessage(index, 1));
        _menu.DeleteFabricatingAction += () => SendPredictedMessage(new LatheAbortFabricationMessage());
    }

    public override void Update()
    {
        base.Update();

        if (_menu == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out LatheComponent? lathe))
            return;

        _menu.Recipes = lathe.Recipes;
        _menu.PopulateRecipes();
        _menu.UpdateCategories();
        _menu.PopulateQueueList(lathe.Queue);
        _menu.SetQueueInfo(lathe.CurrentRecipe);
    }
}
