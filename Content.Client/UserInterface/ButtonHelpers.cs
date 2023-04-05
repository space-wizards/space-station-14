using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface
{
    public static class ButtonHelpers
    {
        /// <summary>
        /// This searches recursively through all the children of "parent"
        /// and sets the Disabled value of any buttons found to "val"
        /// </summary>
        /// <param name="parent">The control which childrens get searched</param>
        /// <param name="val">The value to which disabled gets set</param>
        public static void SetButtonDisabledRecursive(Control parent, bool val)
        {
            foreach (var child in parent.Children)
            {
                if (child is Button but)
                {
                    but.Disabled = val;
                    continue;
                }

                if (child.ChildCount > 0)
                {
                    SetButtonDisabledRecursive(child, val);
                }
            }
        }
    }
}
