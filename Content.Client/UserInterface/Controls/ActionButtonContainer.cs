using Content.Client.UserInterface.Controllers;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class ActionButtonContainer : GridContainer
{
    private int? _buttonCount;

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IUIControllerManager _uiControllerManager = default!;

    public ActionButtonContainer()
    {
        IoCManager.InjectDependencies(this);
        _uiControllerManager.GetController<ActionUIController>().RegisterActionContainer(this);
    }

    public ActionButton this[int index]
    {
        get => (ActionButton)GetChild(index);
        set
        {
            AddChild(value);
            value.SetPositionInParent(index);
        }
    }
    public void LoadActionData(params ActionType?[] actionTypes)
    {
        for (var i = 0; i < actionTypes.Length; i++)
        {
            var action= actionTypes[i];
            if (action == null) continue;
            ((ActionButton)GetChild(i)).UpdateButtonData(_entityManager, action);
        }
    }
    public void ClearActionData()
    {
        foreach (var button in Children)
        {
            ((ActionButton)button).ClearButtonData();
        }
    }

    ~ActionButtonContainer()
    {
        _uiControllerManager.GetController<ActionUIController>().ClearActionContainer();
    }

}
