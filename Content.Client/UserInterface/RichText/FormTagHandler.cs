using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface.RichText;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;
using Content.Client.Paper.UI;
using Robust.Client.Graphics;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Converts [form] tags into clickable buttons that open fill-in dialogs.
/// </summary>
public sealed class FormTagHandler : IMarkupTagHandler
{
    public string Name => "form";

    public bool CanHandle(MarkupNode node)
    {
        return node.Name == "form" || node.Value.StringValue?.StartsWith("__FORM_") == true;
    }

    private static int _formCounter = 0;
    private static readonly Dictionary<string, int> _formPositions = new();
    private static string _lastText = "";
    
    /// <summary>
    /// Font line height set by PaperWindow to ensure buttons match text height
    /// </summary>
    public static float FontLineHeight { get; set; } = 16.0f; // Default fallback

    private static int GetFormIndex(MarkupNode node)
    {
        return _formCounter++;
    }

    /// <summary>
    /// Resets the form counter to ensure consistent indexing across renders.
    /// </summary>
    public static void ResetFormCounter()
    {
        _formCounter = 0;
    }

    /// <summary>
    /// Counts form buttons before the clicked button to determine which [form] tag it represents.
    /// </summary>
    private static int CountFormButtonsBefore(Control clickedButton)
    {
        var count = 0;
        var root = clickedButton;

        // Find the root container
        while (root.Parent != null)
            root = root.Parent;

        // Count form buttons in document order
        var found = false;
        CountFormButtonsRecursive(root, clickedButton, ref count, ref found);
        return found ? count : 0;
    }

    private static void CountFormButtonsRecursive(Control control, Control target, ref int count, ref bool found)
    {
        if (found) return;

        if (control is Button btn && btn.Text == Loc.GetString("paper-form-fill-button"))
        {
            if (control == target)
            {
                found = true;
                return;
            }
            count++;
        }

        foreach (Control child in control.Children)
        {
            CountFormButtonsRecursive(child, target, ref count, ref found);
        }
    }

    /// <summary>
    /// Caches form tag positions to avoid recalculating on every render.
    /// </summary>
    public static void SetFormText(string text)
    {
        if (_lastText != text)
        {
            _formPositions.Clear();
            _lastText = text;
            var pos = 0;
            var index = 0;
            while ((pos = text.IndexOf("[form]", pos)) != -1)
            {
                _formPositions[pos.ToString()] = index++;
                pos += 6;
            }
        }
    }

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context) { }
    public string TextBefore(MarkupNode node) => "";
    public string TextAfter(MarkupNode node) => "";

    /// <summary>
    /// Creates a clickable button to replace the [form] tag.
    /// </summary>
    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        var btn = new Button
        {
            Text = "Fill",
            MinSize = new Vector2(32, FontLineHeight + 2),
            MaxSize = new Vector2(32, FontLineHeight + 2),
            Margin = new Thickness(1, 0, 1, 0),
            StyleClasses = { "ButtonSquare" },
            TextAlign = Label.AlignMode.Center
        };

        var formIndex = GetFormIndex(node);
        btn.Name = $"form_{formIndex}";

        btn.OnPressed += _ =>
        {
            // Find the PaperWindow parent
            var parent = btn.Parent;
            while (parent != null && parent is not PaperWindow)
                parent = parent.Parent;

            if (parent is PaperWindow paperWindow)
            {
                // Count buttons to determine which [form] tag this represents
                var buttonIndex = CountFormButtonsBefore(btn);
                paperWindow.OpenFormDialog(buttonIndex);
            }
        };

        control = btn;
        return true;
    }
}
