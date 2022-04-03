using Content.Client.Actions;
using Content.Client.Outline;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controllers;

public sealed class ActionUIController : UIController
{
    [UISystemDependency] private readonly ActionsSystem _actionsSystem = default!;
    [UISystemDependency] private readonly InteractionOutlineSystem _interactionOutlineSystem = default!;
    [UISystemDependency] private readonly TargetOutlineSystem _targetOutlineSystem = default!;
    private ActionButtonContainer? _actionbar;
    private List<ActionsTab> _tabs = new();


    public void RegisterActionBar(ActionButtonContainer actionBar)
    {
        _actionbar = actionBar;
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
    }

    private void ActionSystemShutdown()
    {
    }



    private struct ActionsTab
    {
        public readonly Dictionary<string, ActionButton> Buttons = new();
    }

}
