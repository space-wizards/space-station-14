using Content.Client.Stylesheets;
using Content.Shared.Power;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;

namespace Content.Client.Power;

public sealed partial class PowerMonitoringWindow
{
    private void UpdateAllConsoleEntries
        (GridContainer masterContainer,
        PowerMonitoringConsoleEntry[] entries,
        PowerMonitoringConsoleEntry[]? focusSources,
        PowerMonitoringConsoleEntry[]? focusLoads)
    {
        // Remove excess children
        while (masterContainer.ChildCount > entries.Length)
        {
            masterContainer.RemoveChild(masterContainer.GetChild(masterContainer.ChildCount - 1));
        }

        if (!entries.Any())
            return;

        // Add missing children
        while (masterContainer.ChildCount < entries.Length)
        {
            // Basic entry
            var entry = entries[masterContainer.ChildCount];
            var windowEntry = new PowerMonitoringWindowEntry(entry);
            masterContainer.AddChild(windowEntry);

            // Selection action
            windowEntry.Button.OnButtonUp += args => { ButtonAction(windowEntry, masterContainer); };
        }

        // Update all children
        foreach (var child in masterContainer.Children)
        {
            if (child is not PowerMonitoringWindowEntry)
                continue;

            var castChild = (PowerMonitoringWindowEntry) child;

            if (castChild == null)
                continue;

            var entry = entries[child.GetPositionInParent()];
            var uid = _entManager.GetEntity(entry.NetEntity);

            // Update entry info and button appearance
            castChild.Entry = entry;
            UpdateEntryButton(uid, castChild.Button, entry, true);

            // Update sources and loads (if applicable)
            if (_trackedEntity == uid)
            {
                castChild.MainContainer.Visible = true;
                UpdateEntrySourcesAndLoads(masterContainer, castChild.SourcesContainer, focusSources, PowerMonitoringHelper.SourceIconPath);
                UpdateEntrySourcesAndLoads(masterContainer, castChild.LoadsContainer, focusLoads, PowerMonitoringHelper.LoadIconPath);
            }

            else
                castChild.MainContainer.Visible = false;
        }
    }

    public void UpdateEntryButton(EntityUid uid, PowerMonitoringButton button, PowerMonitoringConsoleEntry entry, bool offsetPowerLabel = false)
    {
        // Update button style
        if (uid == _trackedEntity)
            button.AddStyleClass(StyleNano.StyleClassButtonColorGreen);

        else
            button.RemoveStyleClass(StyleNano.StyleClassButtonColorGreen);

        // Update tool tip
        button.ToolTip = Loc.GetString(entry.NameLocalized);

        // Update sprite view
        button.SpriteView.SetEntity(uid);

        // Update name length
        float offset = offsetPowerLabel ? 38f : 0f;
        button.NameLocalized.SetWidth = 220f + offset;

        // Update name
        var name = Loc.GetString(entry.NameLocalized);
        var charLimit = (int) (button.NameLocalized.SetWidth / 8f);

        // Shorten name if required
        if (name.Length > charLimit)
            name = $"{name.Substring(0, charLimit - 3)}...";

        button.NameLocalized.Text = name;

        // Update power value
        button.PowerValue.Text = Loc.GetString("power-monitoring-window-value", ("value", entry.PowerValue));
    }

    private void UpdateEntrySourcesAndLoads(GridContainer masterContainer, GridContainer currentContainer, PowerMonitoringConsoleEntry[]? entries, string iconPath)
    {
        if (currentContainer == null)
            return;

        if (entries == null || !entries.Any())
            return;

        // Remove excess children
        while (currentContainer.ChildCount > entries.Length)
        {
            currentContainer.RemoveChild(currentContainer.GetChild(currentContainer.ChildCount - 1));
        }

        // Add missing children
        while (currentContainer.ChildCount < entries.Length)
        {
            var entry = entries[currentContainer.ChildCount];
            var subEntry = new PowerMonitoringWindowSubEntry();
            currentContainer.AddChild(subEntry);

            // Selection action
            //subEntry.Button.OnButtonUp += args => { ButtonAction(windowEntry, masterContainer); };
        }

        // Update all children
        foreach (var child in currentContainer.Children)
        {
            if (child is not PowerMonitoringWindowSubEntry)
                continue;

            var castChild = (PowerMonitoringWindowSubEntry) child;

            if (castChild == null)
                continue;

            if (castChild.Icon != null)
                castChild.Icon.Texture = _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(iconPath)));

            var entry = entries[child.GetPositionInParent()];
            var uid = _entManager.GetEntity(entry.NetEntity);

            UpdateEntryButton(uid, castChild.Button, entries.ElementAt(child.GetPositionInParent()));
        }
    }

    private void ButtonAction(PowerMonitoringWindowEntry entry, GridContainer masterContainer)
    {
        // Toggle off button?
        if (entry.EntityUid == _trackedEntity)
        {
            entry.Button.RemoveStyleClass(StyleNano.StyleClassButtonColorGreen);
            _trackedEntity = null;

            return;
        }

        // Otherwise, toggle on
        entry.Button.AddStyleClass(StyleNano.StyleClassButtonColorGreen);

        // Toggle off the old button (if applicable)
        if (_trackedEntity != null)
        {
            foreach (PowerMonitoringWindowEntry sibling in masterContainer.Children)
            {
                if (sibling.EntityUid == _trackedEntity)
                {
                    sibling.Button.RemoveStyleClass(StyleNano.StyleClassButtonColorGreen);
                    break;
                }
            }
        }

        // Center the nav map on selected entity
        _trackedEntity = entry.EntityUid;

        if (entry.Entry.Coordinates != null)
            NavMap.CenterToCoordinates(_entManager.GetCoordinates(entry.Entry.Coordinates.Value));

        // Switch tabs
        switch (entry.Entry.Group)
        {
            case PowerMonitoringConsoleGroup.Generator:
                MasterTabContainer.CurrentTab = 0; break;
            case PowerMonitoringConsoleGroup.SMES:
                MasterTabContainer.CurrentTab = 1; break;
            case PowerMonitoringConsoleGroup.Substation:
                MasterTabContainer.CurrentTab = 2; break;
            case PowerMonitoringConsoleGroup.APC:
                MasterTabContainer.CurrentTab = 3; break;
        }

        // Get the scroll position of the selected entity on the selected button the UI
        TryGetNextScrollValue(out _nextScrollValue);

        // Request an update from the power monitoring system
        RequestPowerMonitoringUpdateAction?.Invoke(_entManager.GetNetEntity(_trackedEntity));
        _updateTimer = 0f;
    }

    private bool TryGetNextScrollValue(out float? nextScrollValue)
    {
        nextScrollValue = null;

        var scroll = MasterTabContainer.Children.ElementAt(MasterTabContainer.CurrentTab) as ScrollContainer;
        if (scroll == null)
            return false;

        var grid = scroll.Children.ElementAt(0) as GridContainer;
        if (grid == null)
            return false;

        var pos = grid.Children.FirstOrDefault(x => (x is PowerMonitoringWindowEntry) && ((PowerMonitoringWindowEntry) x).EntityUid == _trackedEntity);
        if (pos == null)
            return false;

        nextScrollValue = 40f * pos.GetPositionInParent();

        return true;
    }

    private void TryToScrollToFocus()
    {
        if (_nextScrollValue != null)
        {
            var scroll = MasterTabContainer.Children.ElementAt(MasterTabContainer.CurrentTab) as ScrollContainer;
            if (scroll == null)
                return;

            scroll.SetScrollValue(new Vector2(0, _nextScrollValue.Value));

            _nextScrollValue = null;
        }
    }
}

public sealed class PowerMonitoringWindowEntry : BoxContainer
{
    public EntityUid EntityUid;
    public PowerMonitoringConsoleEntry Entry;
    public PowerMonitoringButton Button;
    public GridContainer MainContainer;
    public GridContainer SourcesContainer;
    public GridContainer LoadsContainer;

    public PowerMonitoringWindowEntry(PowerMonitoringConsoleEntry entry)
    {
        Entry = entry;

        // Alignment
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        // Add selection button
        Button = new PowerMonitoringButton();
        Button.StyleClasses.Add("OpenLeft");
        AddChild(Button);

        // Grid container to hold sub containers
        MainContainer = new GridContainer();
        MainContainer.Columns = 1;
        MainContainer.HorizontalExpand = true;
        MainContainer.Margin = new Thickness(8, 5, 0, 0);
        MainContainer.Visible = false;
        AddChild(MainContainer);

        // Grid container to hold the list of sources when selected 
        SourcesContainer = new GridContainer();
        SourcesContainer.Columns = 1;
        SourcesContainer.HorizontalExpand = true;
        SourcesContainer.Margin = new Thickness(0, 0, 0, 0);
        MainContainer.AddChild(SourcesContainer);

        // Grid container to hold the list of loads when selected
        LoadsContainer = new GridContainer();
        LoadsContainer.Columns = 1;
        LoadsContainer.HorizontalExpand = true;
        LoadsContainer.Margin = new Thickness(0, 0, 0, 0);
        MainContainer.AddChild(LoadsContainer);

        // Add spacer
        var spacer = new Control();
        spacer.Margin = new Thickness(0, 0, 0, 5);
        AddChild(spacer);
    }
}

public sealed class PowerMonitoringWindowSubEntry : BoxContainer
{
    public TextureRect? Icon;
    public PowerMonitoringButton Button;

    public PowerMonitoringWindowSubEntry()
    {
        Orientation = LayoutOrientation.Horizontal;
        HorizontalExpand = true;

        Icon = new TextureRect();
        Icon.VerticalAlignment = VAlignment.Center;
        Icon.Margin = new Thickness(0, 0, 2, 0);
        AddChild(Icon);

        Button = new PowerMonitoringButton();
        Button.StyleClasses.Add("OpenBoth");
        AddChild(Button);
    }
}

public sealed class PowerMonitoringButton : Button
{
    public GridContainer MainContainer;
    public SpriteView SpriteView;
    public Label NameLocalized;
    public Label PowerValue;

    public PowerMonitoringButton()
    {
        HorizontalExpand = true;
        VerticalExpand = true;

        MainContainer = new GridContainer() { Columns = 3 };
        AddChild(MainContainer);

        SpriteView = new SpriteView();
        SpriteView.OverrideDirection = Direction.South;
        SpriteView.SetSize = new Vector2(32f, 32f);
        MainContainer.AddChild(SpriteView);

        NameLocalized = new Label();
        NameLocalized.ClipText = true;
        NameLocalized.SetWidth = 220f;
        MainContainer.AddChild(NameLocalized);

        PowerValue = new Label();
        PowerValue.ClipText = true;
        PowerValue.SetWidth = 64f;
        MainContainer.AddChild(PowerValue);
    }
}
