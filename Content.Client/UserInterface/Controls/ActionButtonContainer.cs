using Content.Client.HUD;
using Content.Client.UserInterface.Controllers;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controls;

public sealed class ActionButtonContainer : ItemSlotUIContainer<ActionButton>
{
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
    }
    public Page CreateNewTab(params ActionButton?[] buttons)
    {
        Page newPage = new(buttons);
        _tabs.Add(newPage);
        return newPage;
    }

    private void Internal_LoadTab(int page)
    {//internally called only, no need for error checking

    }

    public void SetButtonOnSlot(ActionButton button, int pageSlotIndex, int page = 0)
    {
        if (page >= _tabs.Count || pageSlotIndex >= _tabs[page].Count) return;
        _tabs[page][pageSlotIndex] = button;
    }

    private void UnloadPage(int page)
    {//internally called only, no need for error checking
        foreach (var button in (List<ActionButton?>)_tabs[page])
        {
            if (button != null) RemoveChild(button);
            //TODO: add dummy buttons
        }
    }

    private void LoadPage(int page)
    {//internally called only, no need for error checking
        foreach (var button in (List<ActionButton?>)_tabs[page])
        {
            if (button != null)
            {
                AddChild(button);

            }
            else
            {
                //TODO: Implemenent dummy buttons
            }
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
        public List<ActionButton?> actionSlots;
        //TODO: dummy buttons

        public Page()
        {
            actionSlots = new();
        }

        public Page(params ActionButton?[] buttons)
        {
            actionSlots = new(buttons);
        }

        public ActionButton?[] ToArray()
        {
            return actionSlots.ToArray();
        }

        public int Count => actionSlots.Count;

        public ActionButton? this[int key]
        {
            get => actionSlots[key];
            set => actionSlots[key] = value;
        }
        public static explicit operator List<ActionButton?>(Page a)
        {
            return a.actionSlots;
        }
    }

    protected override void OnThemeUpdated()
    {

    }
}
