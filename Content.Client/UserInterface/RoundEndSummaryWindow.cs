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

#pragma warning disable 649
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly ILocalizationManager _loc;
        [Dependency] private readonly IInputManager _inputManager;
#pragma warning restore 649

        public RoundEndSummaryWindow(string gm, uint duration)
        {
            Title = "Round End Summary";

            //Get section header font
            _loc = IoCManager.Resolve<ILocalizationManager>();
            var cache = IoCManager.Resolve<IResourceCache>();
            var inputManager = IoCManager.Resolve<IInputManager>();
            Font headerFont = new VectorFont(cache.GetResource<FontResource>("/Nano/NotoSans/NotoSans-Regular.ttf"), _headerFontSize);

            var scrollContainer = new ScrollContainer();
            scrollContainer.AddChild(VBox = new VBoxContainer());
            Contents.AddChild(scrollContainer);

            //Gamemode Name
            var gamemodeLabel = new RichTextLabel();
            gamemodeLabel.SetMarkup(_loc.GetString("Round of: [color=white]{0}[/color] has ended.", gm));
            VBox.AddChild(gamemodeLabel);

            //Duration
            var roundDurationInfo = new RichTextLabel();
            roundDurationInfo.SetMarkup(_loc.GetString("The round lasted for [color=yellow]{0}[/color] hours.", duration));
            VBox.AddChild(roundDurationInfo);

            OpenCentered();
            MoveToFront();

        }
    }
}
