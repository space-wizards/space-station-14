using System.Linq;
using Content.Client.HUD;
using Content.Client.UserInterface.Controllers;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

public sealed class ActionButtonContainer : ItemSlotUIContainer<ActionButton>
{
    private GridContainer _grid;
    private int _selectedTab;
    public int ActiveTab
    {
        get => _selectedTab;
        set
        {
            if (value >= _tabs.Count) return;
            _selectedTab = value;
        }
    }

    private readonly List<Page> _tabs = new();

    public ActionButtonContainer()
    {
        Orientation = LayoutOrientation.Vertical;
        IoCManager.Resolve<IUIControllerManager>().GetController<ActionUIController>().RegisterActionBar(this);
        _grid = new GridContainer
        {
            Columns   = 1
        };
        AddChild(_grid);

    }
    public Page CreateNewPage(params ActionButton[] buttons)
    {
        Page newPage = new(buttons);
        _tabs.Add(newPage);
        return newPage;
    }

    public void RemovePage(int index = -1)
    {
        if (index >= _tabs.Count) return;
        if (index < 0) index = _tabs.Count - 1;

        UnloadPage(index);
        _tabs[index].ClearButtons();
        _tabs.RemoveAt(index);

    }

    public void SetButtonOnSlot(ActionButton button, int pageSlotIndex, int page = 0)
    {
        if (page >= _tabs.Count || pageSlotIndex >= _tabs[page].Count) return;
        _tabs[page][pageSlotIndex] = button;
    }

    private void UnloadPage(int page)
    {
        foreach (var button in (List<ActionButton>)_tabs[page])
        {
            if (button != null) _grid.RemoveChild(button);
        }
    }

    private void LoadPage(int page)
    {
        foreach (var button in (List<ActionButton>)_tabs[page])
        {
            _grid.AddChild(button);
        }
    }

    public override ActionButton? AddButton(ActionButton newButton)
    {
        return AddButtonToDict(newButton);
    }

    public override void RemoveButton(ActionButton button)
    {
        RemoveButtonFromDict(button);
        button.Dispose();
    }

    //NOTE: This only removes the page, and removes the buttons. This does NOT dispose of the buttons.
    //This will cause duplicate slotName issues if not properly handled.
    public void RemoveTab(int page)
    {
        if (page >= _tabs.Count) return;
        _tabs.RemoveAt(page);
    }

    //This will clear the specified tab AND remove the buttons
    public void ClearTab(int page)
    {
        if (page >= _tabs.Count) return;
        RemoveButtons(_tabs[page].ToArray());
        RemoveTab(page);
    }

    public sealed class Page
    {
        public List<ActionButton> actionSlots;
        public Page()
        {
            actionSlots = new();
        }

        public Page(params ActionButton[] buttons)
        {
            actionSlots = new(buttons);
        }

        public ActionButton[] ToArray()
        {
            return actionSlots.ToArray();
        }

        public int Count => actionSlots.Count;

        public void ClearButtons()
        {
            foreach (var button in actionSlots)
            {
                button.Dispose();
            }
        }

        public ActionButton this[int key]
        {
            get => actionSlots[key];
            set => actionSlots[key] = value;
        }
        public static explicit operator List<ActionButton>(Page a)
        {
            return a.actionSlots;
        }
    }
}
