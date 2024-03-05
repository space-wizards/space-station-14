using System.Linq;
using Content.Client.Actions;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Actions.Controls;

[Virtual]
public class ActionButtonContainer : GridContainer
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IInputManager _input = default!;

    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionPressed;
    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionUnpressed;
    public event Action<ActionButton>? ActionFocusExited;

    public ActionButtonContainer()
    {
        IoCManager.InjectDependencies(this);
    }

    public ActionButton this[int index]
    {
        get => (ActionButton) GetChild(index);
    }

    private void BuildActionButtons(int count)
    {
        var keys = ContentKeyFunctions.GetHotbarBoundKeys();

        Children.Clear();
        for (var index = 0; index < count; index++)
        {
            Children.Add(MakeButton(index));
        }

        ActionButton MakeButton(int index)
        {
            var button = new ActionButton(_entity);

            if (keys.TryGetValue(index, out var boundKey))
            {
                button.KeyBind = boundKey;

                var binding = _input.GetKeyBinding(boundKey);
                button.Label.Text = binding.GetKeyString();
            }

            return button;
        }
    }

    public void SetActionData(ActionsSystem system, params EntityUid?[] actionTypes)
    {
        var uniqueCount = Math.Min(system.GetClientActions().Count(), actionTypes.Length + 1);
        if (ChildCount != uniqueCount)
            BuildActionButtons(uniqueCount);

        for (var i = 0; i < uniqueCount; i++)
        {
            if (!actionTypes.TryGetValue(i, out var action))
                action = null;
            ((ActionButton) GetChild(i)).UpdateData(action, system);
        }
    }

    public void ClearActionData()
    {
        foreach (var button in Children)
        {
            ((ActionButton) button).ClearData();
        }
    }

    protected override void ChildAdded(Control newChild)
    {
        base.ChildAdded(newChild);

        if (newChild is not ActionButton button)
            return;

        button.ActionPressed += ActionPressed;
        button.ActionUnpressed += ActionUnpressed;
        button.ActionFocusExited += ActionFocusExited;
    }

    protected override void ChildRemoved(Control newChild)
    {
        if (newChild is not ActionButton button)
            return;

        button.ActionPressed -= ActionPressed;
        button.ActionUnpressed -= ActionUnpressed;
        button.ActionFocusExited -= ActionFocusExited;
    }

    public bool TryGetButtonIndex(ActionButton button, out int position)
    {
        if (button.Parent != this)
        {
            position = 0;
            return false;
        }

        position = button.GetPositionInParent();
        return true;
    }

    public IEnumerable<ActionButton> GetButtons()
    {
        foreach (var control in Children)
        {
            if (control is ActionButton button)
                yield return button;
        }
    }

    ~ActionButtonContainer()
    {
        UserInterfaceManager.GetUIController<ActionUIController>().RemoveActionContainer();
    }
}
