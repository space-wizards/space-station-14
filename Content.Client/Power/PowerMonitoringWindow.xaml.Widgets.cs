using Content.Client.Stylesheets;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace Content.Client.Power;

public sealed partial class PowerMonitoringWindow
{
    private SpriteSpecifier.Texture _sourceIcon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/PowerMonitoring/source_arrow.png"));
    private SpriteSpecifier.Texture _loadIconPath = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/PowerMonitoring/load_arrow.png"));

    private bool _tryToScroll = false;

    private void UpdateAllConsoleEntries
        (BoxContainer masterContainer,
        List<PowerMonitoringConsoleEntry> entries,
        List<PowerMonitoringConsoleEntry>? focusSources,
        List<PowerMonitoringConsoleEntry>? focusLoads)
    {
        // Remove excess children
        while (masterContainer.ChildCount > entries.Count)
        {
            masterContainer.RemoveChild(masterContainer.GetChild(masterContainer.ChildCount - 1));
        }

        if (!entries.Any())
            return;

        // Add missing children
        while (masterContainer.ChildCount < entries.Count)
        {
            // Basic entry
            var entry = entries[masterContainer.ChildCount];
            var windowEntry = new PowerMonitoringWindowEntry(entry);
            masterContainer.AddChild(windowEntry);

            // Selection action
            windowEntry.Button.OnButtonUp += args =>
            {
                windowEntry.SourcesContainer.DisposeAllChildren();
                windowEntry.LoadsContainer.DisposeAllChildren();
                ButtonAction(windowEntry, masterContainer);
            };
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
            castChild.EntityUid = uid;
            castChild.Entry = entry;

            UpdateEntryButton(uid, castChild.Button, entry);

            // Update sources and loads (if applicable)
            if (_focusEntity == uid)
            {
                castChild.MainContainer.Visible = true;

                UpdateEntrySourcesAndLoads(masterContainer, castChild.SourcesContainer, focusSources, _sourceIcon);
                UpdateEntrySourcesAndLoads(masterContainer, castChild.LoadsContainer, focusLoads, _loadIconPath);
            }

            else
                castChild.MainContainer.Visible = false;
        }
    }

    public void UpdateEntryButton(EntityUid uid, PowerMonitoringButton button, PowerMonitoringConsoleEntry entry)
    {
        if (!uid.IsValid())
            return;

        // Update button style
        if (uid == _focusEntity)
            button.AddStyleClass(StyleNano.StyleClassButtonColorGreen);

        else
            button.RemoveStyleClass(StyleNano.StyleClassButtonColorGreen);

        // Update sprite view
        button.SpriteView.SetEntity(uid);

        // Update name
        var meta = _entManager.GetComponent<MetaDataComponent>(uid);
        var name = Loc.GetString(meta.EntityName);
        var charLimit = (int) (button.NameLocalized.Width / 8f);

        if (_entManager.TryGetComponent<NavMapTrackableComponent>(uid, out var trackable) &&
            trackable.ChildOffsets.Any() &&
            meta.EntityPrototype != null)
        {
            name = Loc.GetString("power-monitoring-window-object-array", ("name", meta.EntityPrototype.Name), ("count", trackable.ChildOffsets.Count + 1));
        }

        // Update tool tip
        button.ToolTip = Loc.GetString(name);

        // Shorten name if required
        if (charLimit > 3 && name.Length > charLimit)
            name = $"{name.Substring(0, charLimit - 3)}...";

        button.NameLocalized.Text = name;

        // Update power value
        button.PowerValue.Text = Loc.GetString("power-monitoring-window-value", ("value", entry.PowerValue));
    }

    private void UpdateEntrySourcesAndLoads(BoxContainer masterContainer, BoxContainer currentContainer, List<PowerMonitoringConsoleEntry>? entries, SpriteSpecifier.Texture icon)
    {
        if (currentContainer == null)
            return;

        if (entries == null || !entries.Any())
        {
            currentContainer.RemoveAllChildren();
            return;
        }

        // Remove excess children
        while (currentContainer.ChildCount > entries.Count)
        {
            currentContainer.RemoveChild(currentContainer.GetChild(currentContainer.ChildCount - 1));
        }

        // Add missing children
        while (currentContainer.ChildCount < entries.Count)
        {
            var entry = entries[currentContainer.ChildCount];
            var subEntry = new PowerMonitoringWindowSubEntry(entry);
            currentContainer.AddChild(subEntry);

            // Selection action
            subEntry.Button.OnButtonUp += args => { ButtonAction(subEntry, masterContainer); };
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
                castChild.Icon.Texture = _spriteSystem.Frame0(icon);

            var entry = entries[child.GetPositionInParent()];
            var uid = _entManager.GetEntity(entry.NetEntity);

            castChild.Entry = entry;
            castChild.EntityUid = uid;

            UpdateEntryButton(uid, castChild.Button, entries.ElementAt(child.GetPositionInParent()));
        }
    }

    private void ButtonAction(PowerMonitoringWindowBaseEntry entry, BoxContainer masterContainer)
    {
        // Toggle off button?
        if (entry.EntityUid == _focusEntity)
        {
            entry.Button.RemoveStyleClass(StyleNano.StyleClassButtonColorGreen);
            _focusEntity = null;

            // Request an update from the power monitoring system
            RequestPowerMonitoringUpdateAction?.Invoke(null, null);
            _updateTimer = 0f;

            return;
        }

        // Otherwise, toggle on
        entry.Button.AddStyleClass(StyleNano.StyleClassButtonColorGreen);

        // Toggle off the old button (if applicable)
        if (_focusEntity != null)
        {
            foreach (PowerMonitoringWindowEntry sibling in masterContainer.Children)
            {
                if (sibling.EntityUid == _focusEntity)
                {
                    sibling.Button.RemoveStyleClass(StyleNano.StyleClassButtonColorGreen);
                    break;
                }
            }
        }

        // Center the nav map on selected entity
        _focusEntity = entry.EntityUid;

        //if (!TryGetEntityNavMapTrackingData(entry.EntityUid, out var trackingData))
        //    return;

        if (!_trackedEntities.TryGetValue(entry.EntityUid, out var kvp))
            return;

        var coords = kvp.Item1;
        var trackable = kvp.Item2;

        NavMap.CenterToCoordinates(coords);

        // Switch tabs
        SwitchTabsBasedOnPowerMonitoringConsoleGroup(entry.Entry.Group);

        // Get the scroll position of the selected entity on the selected button the UI
        _tryToScroll = true;

        // Request an update from the power monitoring system
        RequestPowerMonitoringUpdateAction?.Invoke(_entManager.GetNetEntity(_focusEntity), entry.Entry.Group);
        _updateTimer = 0f;
    }

    private bool TryGetNextScrollPosition([NotNullWhen(true)] out float? nextScrollPosition)
    {
        nextScrollPosition = null;

        var scroll = MasterTabContainer.Children.ElementAt(MasterTabContainer.CurrentTab) as ScrollContainer;
        if (scroll == null)
            return false;

        var container = scroll.Children.ElementAt(0) as BoxContainer;
        if (container == null || !container.Children.Any())
            return false;

        // Exit if the heights of the children haven't been initialized yet
        if (!container.Children.Any(x => x.Height > 0))
            return false;

        nextScrollPosition = 0;

        foreach (var control in container.Children)
        {
            if (control == null || control is not PowerMonitoringWindowEntry)
                continue;

            if (((PowerMonitoringWindowEntry) control).EntityUid == _focusEntity)
                return true;

            nextScrollPosition += control.Height;
        }

        // Failed to find control
        nextScrollPosition = null;

        return false;
    }

    private bool TryGetVerticalScrollbar(ScrollContainer scroll, [NotNullWhen(true)] out VScrollBar? vScrollBar)
    {
        vScrollBar = null;

        foreach (var child in scroll.Children)
        {
            if (child is not VScrollBar)
                continue;

            var castChild = child as VScrollBar;

            if (castChild != null)
            {
                vScrollBar = castChild;
                return true;
            }
        }

        return false;
    }

    private void TryToScrollToFocus()
    {
        if (!_tryToScroll)
            return;

        var scroll = MasterTabContainer.Children.ElementAt(MasterTabContainer.CurrentTab) as ScrollContainer;
        if (scroll == null)
            return;

        if (!TryGetVerticalScrollbar(scroll, out var vScrollbar))
            return;

        if (TryGetNextScrollPosition(out float? nextScrollPosition))
        {
            vScrollbar.ValueTarget = nextScrollPosition.Value;

            if (MathHelper.CloseToPercent(vScrollbar.Value, vScrollbar.ValueTarget))
            {
                _tryToScroll = false;
                return;
            }
        }
    }

    private void UpdateWarningLabel(PowerMonitoringFlags flags)
    {
        if (flags == PowerMonitoringFlags.None)
        {
            SystemWarningPanel.Visible = false;
            return;
        }

        var msg = new FormattedMessage();

        if ((flags & PowerMonitoringFlags.RoguePowerConsumer) != 0)
        {
            SystemWarningPanel.PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.Red,
                BorderColor = Color.DarkRed,
                BorderThickness = new Thickness(2),
            };

            msg.AddMarkup(Loc.GetString("power-monitoring-window-rogue-power-consumer"));
            SystemWarningPanel.Visible = true;
        }

        else if ((flags & PowerMonitoringFlags.PowerNetAbnormalities) != 0)
        {
            SystemWarningPanel.PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.Orange,
                BorderColor = Color.DarkOrange,
                BorderThickness = new Thickness(2),
            };

            msg.AddMarkup(Loc.GetString("power-monitoring-window-power-net-abnormalities"));
            SystemWarningPanel.Visible = true;
        }

        SystemWarningLabel.SetMessage(msg);
    }

    private void SwitchTabsBasedOnPowerMonitoringConsoleGroup(PowerMonitoringConsoleGroup group)
    {
        switch (group)
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
    }

    private PowerMonitoringConsoleGroup GetCurrentPowerMonitoringConsoleGroup()
    {
        return (PowerMonitoringConsoleGroup) MasterTabContainer.CurrentTab;
    }
}

public sealed class PowerMonitoringWindowEntry : PowerMonitoringWindowBaseEntry
{
    public BoxContainer MainContainer;
    public BoxContainer SourcesContainer;
    public BoxContainer LoadsContainer;

    public PowerMonitoringWindowEntry(PowerMonitoringConsoleEntry entry) : base(entry)
    {
        Entry = entry;

        // Alignment
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        // Update selection button
        Button.StyleClasses.Add("OpenLeft");
        AddChild(Button);

        // Grid container to hold sub containers
        MainContainer = new BoxContainer();
        MainContainer.Orientation = LayoutOrientation.Vertical;
        MainContainer.HorizontalExpand = true;
        MainContainer.Margin = new Thickness(8, 0, 0, 0);
        MainContainer.Visible = false;
        AddChild(MainContainer);

        // Grid container to hold the list of sources when selected 
        SourcesContainer = new BoxContainer();
        SourcesContainer.Orientation = LayoutOrientation.Vertical;
        SourcesContainer.HorizontalExpand = true;
        MainContainer.AddChild(SourcesContainer);

        // Grid container to hold the list of loads when selected
        LoadsContainer = new BoxContainer();
        LoadsContainer.Orientation = LayoutOrientation.Vertical;
        LoadsContainer.HorizontalExpand = true;
        MainContainer.AddChild(LoadsContainer);
    }
}

public sealed class PowerMonitoringWindowSubEntry : PowerMonitoringWindowBaseEntry
{
    public TextureRect? Icon;

    public PowerMonitoringWindowSubEntry(PowerMonitoringConsoleEntry entry) : base(entry)
    {
        Orientation = LayoutOrientation.Horizontal;
        HorizontalExpand = true;

        // Source/load icon
        Icon = new TextureRect();
        Icon.VerticalAlignment = VAlignment.Center;
        Icon.Margin = new Thickness(0, 0, 2, 0);
        AddChild(Icon);

        // Selection button
        Button.StyleClasses.Add("OpenBoth");
        AddChild(Button);
    }
}

public abstract class PowerMonitoringWindowBaseEntry : BoxContainer
{
    public EntityUid EntityUid;
    public PowerMonitoringConsoleEntry Entry;
    public PowerMonitoringButton Button;

    public PowerMonitoringWindowBaseEntry(PowerMonitoringConsoleEntry entry)
    {
        Entry = entry;

        // Add selection button (properties set by derivative classes)
        Button = new PowerMonitoringButton();
    }
}

public sealed class PowerMonitoringButton : Button
{
    public BoxContainer MainContainer;
    public SpriteView SpriteView;
    public Label NameLocalized;
    public Label PowerValue;

    public PowerMonitoringButton()
    {
        HorizontalExpand = true;
        VerticalExpand = true;
        Margin = new Thickness(0, 1, 0, 1);

        MainContainer = new BoxContainer();
        MainContainer.Orientation = BoxContainer.LayoutOrientation.Horizontal;
        MainContainer.HorizontalExpand = true;
        AddChild(MainContainer);

        SpriteView = new SpriteView();
        SpriteView.OverrideDirection = Direction.South;
        SpriteView.SetSize = new Vector2(32f, 32f);
        SpriteView.Margin = new Thickness(0, 0, 5, 0);
        MainContainer.AddChild(SpriteView);

        NameLocalized = new Label();
        NameLocalized.ClipText = true;
        NameLocalized.HorizontalExpand = true;
        MainContainer.AddChild(NameLocalized);

        PowerValue = new Label();
        PowerValue.ClipText = true;
        PowerValue.SetWidth = 72f;
        PowerValue.HorizontalAlignment = HAlignment.Right;
        MainContainer.AddChild(PowerValue);
    }
}
