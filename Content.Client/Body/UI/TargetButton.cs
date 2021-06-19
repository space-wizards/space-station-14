using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;

namespace Content.Client.Body.UI
{
    public class TargetButton : Button
    {
        public TargetButton(EntityUid target)
        {
            Target = target;
        }

        public EntityUid Target { get; }
    }
}
