using Robust.Client.Graphics;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Content.Client.Utility;

namespace Content.Client.UserInterface
{
    public sealed class RoundEndSummaryWindow : SS14Window
    {
        private readonly int _headerFontSize = 14;
        private VBoxContainer VBox { get; }

        protected override Vector2? CustomSize => (520, 580);

        public RoundEndSummaryWindow(string gm, uint duration)
        {
            Title = Loc.GetString("Round End Summary");

            var cache = IoCManager.Resolve<IResourceCache>();
            var inputManager = IoCManager.Resolve<IInputManager>();
            Font headerFont = new VectorFont(cache.GetResource<FontResource>("/Nano/NotoSans/NotoSans-Regular.ttf"), _headerFontSize);

            var scrollContainer = new ScrollContainer();
            scrollContainer.AddChild(VBox = new VBoxContainer());
            Contents.AddChild(scrollContainer);

            //Gamemode Name
            var gamemodeLabel = new RichTextLabel();
            gamemodeLabel.SetMarkup(Loc.GetString("Round of: [color=white]{0}[/color] has ended.", gm));
            VBox.AddChild(gamemodeLabel);

            //Duration
            var roundDurationInfo = new RichTextLabel();
            roundDurationInfo.SetMarkup(Loc.GetString("The round lasted for [color=yellow]{0}[/color] hours.", duration));
            VBox.AddChild(roundDurationInfo);

            OpenCentered();
            MoveToFront();

        }
    }
}
