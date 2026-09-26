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
            Group = group,
            ToolTip = tab.Icon == null ? null : tab.Name
        };
        button.OnPressed += _ => tab.OnSelected();

        if (ButtonSize is { } size)
        {
            DebugTools.Assert(size.X >= 0 && size.Y >= 0);
            button.SetSize = size;
        }

        if (ButtonStyleClass is { } styleClass)
            button.AddStyleClass(styleClass);

        if (tab.Icon is not { } icon)
        {
            button.Text = tab.Name;
            return button;
        }

        var textureRect = new TextureRect
        {
            Texture = icon.Texture,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = icon.Size ?? new Vector2(32),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center
        };

        if (icon.StyleClass != null)
            textureRect.AddStyleClass(icon.StyleClass);

        button.AddChild(textureRect);

        return button;
    }
}

/// <summary>
/// Button used by <see cref="TabBar"/> so stylesheets can target its tabs.
/// </summary>
public sealed class TabBarButton : Button;

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
