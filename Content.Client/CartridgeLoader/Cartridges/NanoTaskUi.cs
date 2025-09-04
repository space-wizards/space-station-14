using System.Linq;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

/// <summary>
///     UI fragment responsible for displaying NanoTask controls in a PDA and coordinating with the NanoTaskCartridgeSystem for state
/// </summary>
public sealed partial class NanoTaskUi : UIFragment
{
    private NanoTaskUiFragment? _fragment;
    private NanoTaskItemPopup? _popup;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new NanoTaskUiFragment();
        _popup = new NanoTaskItemPopup();
        _fragment.NewTask += () =>
        {
            _popup.ResetInputs(null);
            _popup.SetEditingTaskId(null);
            _popup.OpenCentered();
        };
        _fragment.OpenTask += id =>
        {
            if (_fragment.Tasks.Find(task => task.Id == id) is not NanoTaskItemAndId task)
                return;

            _popup.ResetInputs(task.Data);
            _popup.SetEditingTaskId(task.Id);
            _popup.OpenCentered();
        };
        _fragment.ToggleTaskCompletion += id =>
        {
            if (_fragment.Tasks.Find(task => task.Id == id) is not NanoTaskItemAndId task)
                return;

            userInterface.SendMessage(new CartridgeUiMessage(new NanoTaskUiMessageEvent(new NanoTaskUpdateTask(new(id, new(
                description: task.Data.Description,
                taskIsFor: task.Data.TaskIsFor,
                isTaskDone: !task.Data.IsTaskDone,
                priority: task.Data.Priority
            ))))));
        };
        _popup.TaskSaved += (id, data) =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new NanoTaskUiMessageEvent(new NanoTaskUpdateTask(new(id, data)))));
            _popup.Close();
        };
        _popup.TaskDeleted += id =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new NanoTaskUiMessageEvent(new NanoTaskDeleteTask(id))));
            _popup.Close();
        };
        _popup.TaskCreated += data =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new NanoTaskUiMessageEvent(new NanoTaskAddTask(data))));
            _popup.Close();
        };
        _popup.TaskPrinted += data =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new NanoTaskUiMessageEvent(new NanoTaskPrintTask(data))));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not NanoTaskUiState nanoTaskState)
            return;

        _fragment?.UpdateState(nanoTaskState.Tasks);
    }
}
