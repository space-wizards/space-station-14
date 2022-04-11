using Content.Client.Actions;
using Content.Client.Outline;
using Content.Client.UserInterface.Controls;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controllers;

public sealed class ActionUIController : UIController
{
    [UISystemDependency] private readonly ActionsSystem _actionsSystem = default!;
    [UISystemDependency] private readonly InteractionOutlineSystem _interactionOutlineSystem = default!;
    [UISystemDependency] private readonly TargetOutlineSystem _targetOutlineSystem = default!;
    private ActionButtonContainer? actionContainer;
    private ActionPage? _defaultPage;
    private List<ActionPage> _actionPages = new();

    public void RegisterActionContainer(ActionButtonContainer actionBar)
    {
        if (actionContainer != null)
        {
            Logger.Warning("Action container already defined for UI controller");
            return;
        }
        actionContainer = actionBar;
    }
    public void ClearActionContainer()
    {
        actionContainer = null;
    }
    public void ClearActionBars()
    {
        actionContainer = null;
    }
    public override void OnSystemLoaded(IEntitySystem system)
    {
        if (system is ActionsSystem)
        {
            ActionSystemStart();
        }
    }

    public override void OnSystemUnloaded(IEntitySystem system)
    {
        if (system is ActionsSystem)
        {
            ActionSystemShutdown();
        }
    }

    private void ActionSystemStart()
    {
        _actionsSystem.OnLinkActions += OnComponentLinked;
        _actionsSystem.OnUnlinkActions += OnComponentUnlinked;
    }

    private void ActionSystemShutdown()
    {
        _actionsSystem.OnLinkActions = null;
        _actionsSystem.OnUnlinkActions = null;
    }

    private void OnComponentUnlinked()
    {
        _defaultPage = null;
        actionContainer?.ClearActionData();
        //TODO: Clear button data
    }

    private void OnComponentLinked(ActionsComponent component)
    {
        LoadDefaultActions(component);
        if (_defaultPage != null) actionContainer?.LoadActionData(_defaultPage);
    }



    private void LoadDefaultActions(ActionsComponent component)
    {
        List<ActionType> actionsToadd = new();
        foreach (var actionType in component.Actions)
        {
            if (actionType.AutoPopulate)
            {
                actionsToadd.Add(actionType);
            }
        }
        _defaultPage = new ActionPage(actionsToadd.ToArray());
    }



    //TODO: Serialize this shit
    private sealed class ActionPage
    {
        public readonly List<ActionType?> Data;

        public ActionPage(SortedSet<ActionType> actions)
        {
            Data = new List<ActionType?>(actions);
        }

        public ActionPage(params ActionType?[] actions)
        {
            Data = new List<ActionType?>(actions);
        }
        public ActionType? this[int index]
        {
            get => Data[index];
            set => Data[index] = value;
        }

        public static implicit operator ActionType?[](ActionPage p)
        {
            return p.Data.ToArray();
        }
    }

}
