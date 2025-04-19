using System.Linq;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
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
        _fragment.NewTask += (table, category) =>
        {
            _popup.ResetInputs(null);
            _popup.SetEditingTaskId(null);
            _popup.SetCategory(category.Category, name: category.Department);
            _popup.OpenCentered();
        };
        _fragment.OpenTask += (table, category, id) =>
        {
            if (table.Tasks.Find(task => task.Item.Id == id) is not NanoTaskItemAndDepartment task)
                return;

            _popup.ResetInputs(task.Item.Data);
            _popup.SetEditingTaskId(task.Item.Id);
            _popup.SetCategory(category.Category, category.Department);
            _popup.OpenCentered();
        };
        _fragment.ToggleTaskCompletion += (table, category, id) =>
        {
            if (table.Tasks.Find(task => task.Item.Id == id) is not NanoTaskItemAndDepartment task)
                return;

            userInterface.SendMessage(new CartridgeUiMessage(new NanoTaskUiMessageEvent(new NanoTaskUpdateTask(new NanoTaskItemAndDepartment(new NanoTaskItemAndId(id, new NanoTaskItem(
                description: task.Item.Data.Description,
                taskIsFor: task.Item.Data.TaskIsFor,
                status: NanoTaskItemStatus.Completed,
                priority: task.Item.Data.Priority
            )), category)))));
        };
        _popup.TaskSaved += (id, category, data) =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new NanoTaskUiMessageEvent(new NanoTaskUpdateTask(new NanoTaskItemAndDepartment(new NanoTaskItemAndId(id, data), category)))));
            _popup.Close();
        };
        _popup.TaskDeleted += id =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new NanoTaskUiMessageEvent(new NanoTaskDeleteTask(id))));
            _popup.Close();
        };
        _popup.TaskCreated += (category, data) =>
        {
            userInterface.SendMessage(new CartridgeUiMessage(new NanoTaskUiMessageEvent(new NanoTaskAddTask(data, category))));
            _popup.Close();
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is NanoTaskServerOfflineUiState)
            _fragment?.UpdateStateNoServers();

        if (state is not NanoTaskUiState nanoTaskState)
            return;

        _fragment?.UpdateState(nanoTaskState.Tasks, nanoTaskState.Departments);
    }
}
