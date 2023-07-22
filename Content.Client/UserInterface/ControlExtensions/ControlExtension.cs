using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.ControlExtensions
{
    public static class ControlExtension
    {
        public static List<T> GetControlOfType<T> (this Control parent, string childType) where T : Control
        {
            List<T> controlList = new List<T>();

            foreach (var child in parent.Children)
            {
                if (child.GetType().Name == childType)
                {
                    controlList.Add((T) child);
                }

                if (child.ChildCount > 0)
                {
                    controlList.AddRange(child.GetControlOfType<T>(childType));
                }
            }

            return controlList;
        }

        public static bool ChildrenContainText(this Control parent, string search)
        {
            var labels = parent.GetControlOfType<Label>("Label");
            var richTextLabels = parent.GetControlOfType<RichTextLabel>("RichTextLabel");

            foreach (var label in labels)
            {
                if (label.Text != null && label.Text.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            foreach (var label in richTextLabels)
            {
                var message = label.GetMessage();

                if (message != null && message.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
