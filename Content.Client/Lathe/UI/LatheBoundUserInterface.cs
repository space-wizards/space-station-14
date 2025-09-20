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

    public override void Update()
    {
        base.Update();

        if (_menu == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out LatheComponent? comp))
            return;

        _menu.Recipes = comp.Recipes;
        _menu.PopulateRecipes();
        _menu.UpdateCategories();
        _menu.PopulateQueueList(comp.Queue);
        _menu.SetQueueInfo(comp.CurrentRecipe);
    }
}
