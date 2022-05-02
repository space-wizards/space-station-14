using Content.Client.UserInterface.Controllers;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class ActionButtonContainer : GridContainer
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionPressed;
    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionUnpressed;
    public event Action<ActionButton>? ActionFocusExited;

    public ActionButtonContainer()
    {
        IoCManager.InjectDependencies(this);
        _ui.GetUIController<ActionUIController>().RegisterActionContainer(this);
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
            value.ActionFocusExited += ActionFocusExited;
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
            button.ActionFocusExited += ActionFocusExited;
        }
    }

    ~ActionButtonContainer()
    {
        _ui.GetUIController<ActionUIController>().ClearActionContainer();
    }
}
