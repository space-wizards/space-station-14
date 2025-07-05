using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface.RichText;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;
using Content.Client.Paper.UI;

namespace Content.Client.UserInterface.RichText
{
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

        private static int GetFormIndex(MarkupNode node)
        {
            return _formCounter++;
        }

        public static void ResetFormCounter()
        {
            _formCounter = 0;
        }
        
        private static int CountFormButtonsBefore(Control clickedButton)
        {
            var count = 0;
            var root = clickedButton;
            
            // Find the root container
            while (root.Parent != null)
                root = root.Parent;
            
            // Count form buttons in document order (top to bottom, left to right)
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

        public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
        {
            var btn = new Button
            {
                Text = Loc.GetString("paper-form-fill-button"),
                MinSize = new Vector2(50, 28),
                MaxSize = new Vector2(50, 28),
                Margin = new Thickness(4, 2, 4, 2)
            };

            // Store form index in the button's Name property
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
                    // Count which button this is by walking the UI tree
                    var buttonIndex = CountFormButtonsBefore(btn);
                    paperWindow.OpenFormDialog(buttonIndex);
                }
            };

            control = btn;
            return true;
        }
    }
}
