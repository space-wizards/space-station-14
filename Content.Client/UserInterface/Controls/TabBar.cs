using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Displays mutually exclusive tabs using text or an icon.
/// Pressing a tab invokes its selection action without managing tab content.
/// </summary>
public sealed class TabBar : BoxContainer
{
    /// <summary>
    /// Adds a style class to each tab button.
    /// </summary>
    public string? ButtonStyleClass { get; set; }

    /// <summary>
    /// Sets the size of each tab button, overriding the stylesheet.
    /// </summary>
    public Vector2? ButtonSize { get; set; }

    /// <summary>
    /// Rebuilds the tabs. Marks the tab at <paramref name="selectedIndex"/>
    /// as pressed without invoking its action.
    /// </summary>
    public void SetTabs(IReadOnlyList<TabBarEntry> tabs, int selectedIndex)
    {
        RemoveAllChildren();

        var group = new ButtonGroup();

        for (var index = 0; index < tabs.Count; index++)
        {
            var button = CreateButton(tabs[index], group);

            AddChild(button);

            if (index == selectedIndex)
                button.Pressed = true;
        }
    }

    private TabBarButton CreateButton(TabBarEntry tab, ButtonGroup group)
    {
        var button = new TabBarButton
        {
            Group = group
        };
        button.SetTabInfo(tab);
        button.OnPressed += _ => tab.OnSelected();

        if (ButtonSize is { } size)
        {
            DebugTools.Assert(size.X >= 0 && size.Y >= 0);
            button.SetSize = size;
        }

        if (ButtonStyleClass is { } styleClass)
            button.AddStyleClass(styleClass);

        return button;
    }
}

/// <summary>
/// Button used by <see cref="TabBar"/> so stylesheets can target its tabs.
/// </summary>
public sealed class TabBarButton : Button
{
    private TextureRect? _iconRect;

    /// <summary> Actual button c-tor. </summary>
    public void SetTabInfo(TabBarEntry tab)
    {
        ToolTip = tab.Icon == null ? null : tab.Name;
        var icon = tab.Icon;
        if (icon == null)
            return;

        if (_iconRect == null)
        {
            _iconRect = new TextureRect();

            AddChild(_iconRect);
        }

        _iconRect.Texture = icon.Texture;
        _iconRect.Stretch = TextureRect.StretchMode.KeepAspectCentered;
        _iconRect.SetSize = icon.Size ?? new Vector2(32);
        _iconRect.HorizontalAlignment = HAlignment.Center;
        _iconRect.VerticalAlignment = VAlignment.Center;

        if (icon.StyleClass != null)
            _iconRect.AddStyleClass(icon.StyleClass);


    }
}

/// <summary>
/// Content and selection action for one tab.
/// </summary>
public sealed record TabBarEntry(
    string Name,
    Action OnSelected,
    TabBarIcon? Icon = null);

/// <summary>
/// Describes a tab icon. Its texture can be supplied by a stylesheet.
/// </summary>
public sealed record TabBarIcon(
    Texture? Texture = null,
    Vector2? Size = null,
    string? StyleClass = null);
