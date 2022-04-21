using Content.Client.UserInterface.Controllers;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class ActionButtonContainer : GridContainer
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IUIControllerManager _uiControllerManager = default!;

    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionPressed;
    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionUnpressed;

    public ActionButtonContainer()
    {
        IoCManager.InjectDependencies(this);
        _uiControllerManager.GetController<ActionUIController>().RegisterActionContainer(this);
    }

    public ActionButton this[int index]
    {
        get => (ActionButton) GetChild(index);
        set
        {
            AddChild(value);
            value.SetPositionInParent(index);
            value.ActionPressed += ActionPressed;
            value.ActionUnpressed += ActionUnpressed;
        }
    }

    public void LoadActionData(params ActionType?[] actionTypes)
    {
        for (var i = 0; i < actionTypes.Length; i++)
        {
            var action = actionTypes[i];
            if (action == null) continue;
            ((ActionButton) GetChild(i)).UpdateButtonData(_entityManager, action);
        }
    }

    public void ClearActionData()
    {
        foreach (var button in Children)
        {
            ((ActionButton) button).ClearButtonData();
        }
    }

    protected override void ChildAdded(Control newChild)
    {
        base.ChildAdded(newChild);

        if (newChild is ActionButton button)
        {
            button.ActionPressed += ActionPressed;
            button.ActionUnpressed += ActionUnpressed;
        }
    }

    ~ActionButtonContainer()
    {
        _uiControllerManager.GetController<ActionUIController>().ClearActionContainer();
    }
}
