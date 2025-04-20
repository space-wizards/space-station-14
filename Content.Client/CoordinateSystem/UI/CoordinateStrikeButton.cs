using Content.Client.BlueArtilery.UI;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.CoordinateSystem.UI
{
    /// <summary>
    ///     LITERALLY just a button that opens the vote call menu.
    ///     Automatically disables itself if the client cannot call votes.
    /// </summary>
    public sealed class CoordinateStrikeButton : Button
    {

        public CoordinateStrikeButton()
        {
            IoCManager.InjectDependencies(this);

            Text = "Aim";
            OnPressed += OnOnPressed;
        }

        private void OnOnPressed(ButtonEventArgs obj)
        {
            var menu = new CoordinateWindow();
            menu.OpenCentered();
        }
    }
}
