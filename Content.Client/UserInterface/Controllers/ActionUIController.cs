using Content.Client.Actions;
using Content.Client.Outline;
using Content.Client.UserInterface.Controls;
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
        LoadDefaultActions();
        if (_defaultPage != null) actionContainer?.LoadActionData(_defaultPage);
    }

    private void ActionSystemShutdown()
    {
        _defaultPage = null;
        actionContainer?.ClearActionData();
    }

    private void LoadDefaultActions()
    {
        if (_actionsSystem.PlayerActions == null) return;
        _defaultPage = new ActionPage(_actionsSystem.PlayerActions.Actions);
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
