using System.Linq;
using Content.Client.Actions;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Actions.Controls;

[Virtual]
public partial class ActionButtonContainer : GridContainer
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IInputManager _input = default!;

    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionPressed;
    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionUnpressed;
    public event Action<ActionButton>? ActionFocusExited;

    // CarpFinder: keep empty hotbar keys so the action grid can show both rows.
    public int MinimumButtons { get; set; }

    public ActionButtonContainer()
    {
        IoCManager.InjectDependencies(this);
    }

    public ActionButton this[int index]
    {
        get => (ActionButton) GetChild(index);
    }

    public void SetActionData(ActionsSystem system, params EntityUid?[] actionTypes)
    {
        var uniqueCount = Math.Min(system.GetClientActions().Count(), actionTypes.Length + 1);
        var keys = ContentKeyFunctions.GetHotbarBoundKeys();
        // CarpFinder: a fixed bar still draws its empty keys.
        var count = Math.Min(keys.Length, Math.Max(uniqueCount, MinimumButtons));

        for (var i = 0; i < count; i++)
        {
            if (i >= ChildCount)
            {
                AddChild(MakeButton(i));
            }

            if (!actionTypes.TryGetValue(i, out var action))
                action = null;
            ((ActionButton) GetChild(i)).UpdateData(action, system);
        }

        for (var i = ChildCount - 1; i >= count; i--)
        {
            RemoveChild(GetChild(i));
        }

        ActionButton MakeButton(int index)
        {
            var button = new ActionButton(_entity);

            if (!keys.TryGetValue(index, out var boundKey))
                return button;

            button.KeyBind = boundKey;
            if (_input.TryGetKeyBinding(boundKey, out var binding))
                button.Label.Text = FormatKeyLabel(index, binding);

            return button;
        }
    }

    /// <summary>
    /// Text drawn on a slot. The action grid drops the modifier from the second row.
    /// </summary>
    protected virtual string FormatKeyLabel(int index, IKeyBinding binding)
    {
        return binding.GetKeyString();
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
}
