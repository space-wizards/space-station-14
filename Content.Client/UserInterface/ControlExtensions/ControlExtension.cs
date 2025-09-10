using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client.Guidebook.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.ControlExtensions;

public static class ControlExtension
{
    public static List<T> GetControlOfType<T>(this Control parent) where T : Control
    {
        return parent.GetControlOfType<T>(typeof(T).Name, false);
    }
    public static List<T> GetControlOfType<T>(this Control parent, string childType) where T : Control
    {
        return parent.GetControlOfType<T>(childType, false);
    }

    public static List<T> GetControlOfType<T>(this Control parent, bool fullTreeSearch) where T : Control
    {
        return parent.GetControlOfType<T>(typeof(T).Name, fullTreeSearch);
    }

    public static List<T> GetControlOfType<T>(this Control parent, string childType, bool fullTreeSearch) where T : Control
    {
        List<T> controlList = new List<T>();

        foreach (var child in parent.Children)
        {
            var isType = child.GetType().Name == childType;
            var hasChildren = child.ChildCount > 0;

            var searchDeeper = hasChildren && !isType;

            if (isType)
            {
                controlList.Add((T) child);
            }

            if (fullTreeSearch || searchDeeper)
            {
                controlList.AddRange(child.GetControlOfType<T>(childType, fullTreeSearch));
            }
        }

        return controlList;
    }

    public static List<ISearchableControl> GetSearchableControls(this Control parent, bool fullTreeSearch = false)
    {
        List<ISearchableControl> controlList = new List<ISearchableControl>();

        foreach (var child in parent.Children)
        {
            var hasChildren = child.ChildCount > 0;
            var searchDeeper = hasChildren && child is not ISearchableControl;

            if (child is ISearchableControl searchableChild)
            {
                controlList.Add(searchableChild);
            }

            if (fullTreeSearch || searchDeeper)
            {
                controlList.AddRange(child.GetSearchableControls(fullTreeSearch));
            }
        }

        return controlList;
    }

    /// <summary>
    /// Search the control’s tree for a parent node of type T
    /// E.g. to find the control implementing some event handling interface.
    /// </summary>
    public static bool TryGetParentHandler<T>(this Control child, [NotNullWhen(true)] out T? result)
    {
        for (var control = child; control is not null; control = control.Parent)
        {
            if (control is not T handler)
                continue;

            result = handler;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Find the control’s offset relative to its closest ScrollContainer
    /// Returns null if the control is not in the tree or not visible.
    /// </summary>
    public static Vector2? GetControlScrollPosition(this Control child)
    {
        if (!child.VisibleInTree)
            return null;

        var position = new Vector2();
        var control = child;

        while (control is not null)
        {
            // The scroll container's direct child is re-positioned while scrolling,
            // so we need to ignore its position.
            if (control.Parent is ScrollContainer)
                break;

            position += control.Position;

            control = control.Parent;
        }

        return position;
    }

    public static bool ChildrenContainText(this Control parent, string search)
    {
        var labels = parent.GetControlOfType<Label>();
        var richTextLabels = parent.GetControlOfType<RichTextLabel>();

        foreach (var label in labels)
        {
            if (label.Text != null && label.Text.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var label in richTextLabels)
        {
            var text = label.GetMessage();

            if (text != null && text.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
