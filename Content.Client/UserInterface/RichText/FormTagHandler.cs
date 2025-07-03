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
        private static int _formCounter = 0;

        private static int GetFormIndex(MarkupNode node)
        {
            return _formCounter++;
        }

        public static void ResetFormCounter()
        {
            _formCounter = 0;
        }

        public void PushDrawContext(MarkupNode node, MarkupDrawingContext context) { }
        public void PopDrawContext(MarkupNode node, MarkupDrawingContext context) { }
        public string TextBefore(MarkupNode node) => "";
        public string TextAfter(MarkupNode node) => "";

        public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
        {
            var btn = new Button
            {
                Text = "Fill",
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
                    paperWindow.OpenFormDialog(formIndex);
            };

            control = btn;
            return true;
        }
    }
}
