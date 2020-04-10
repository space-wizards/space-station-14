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
using Robust.Client.Player;
using System.Linq;
using System.Collections.Generic;
using static Robust.Client.UserInterface.Controls.ItemList;
using static Content.Shared.SharedGameTicker;

namespace Content.Client.UserInterface
{
    public sealed class RoundEndSummaryWindow : SS14Window
    {

 #pragma warning disable 649
        [Dependency] private IPlayerManager _playerManager;
#pragma warning restore 649

        private readonly int _headerFontSize = 14;
        private VBoxContainer VBox { get; }
        private ItemList _playerList;

        protected override Vector2? CustomSize => (520, 580);

        public RoundEndSummaryWindow(string gm, uint duration, List<RoundEndPlayerInfo> info )
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
            gamemodeLabel.SetMarkup(Loc.GetString("Round of [color=white]{0}[/color] has ended.", gm));
            VBox.AddChild(gamemodeLabel);

            //Duration
            var roundDurationInfo = new RichTextLabel();
            roundDurationInfo.SetMarkup(Loc.GetString("The round lasted for [color=yellow]{0}[/color] hours.", duration));
            VBox.AddChild(roundDurationInfo);


            //Populate list of players.
            _playerManager = IoCManager.Resolve<IPlayerManager>();
            _playerList = new ItemList()
            {
                SizeFlagsStretchRatio = 8,
                SizeFlagsVertical = SizeFlags.FillExpand,
                SelectMode = ItemList.ItemListSelectMode.Button
            };

            foreach(var plyinfo in info)
            {
                var oocName = plyinfo.PlayerOOCName;
                var icName = plyinfo.PlayerICName;
                var role = plyinfo.Role;
                var wasAntag = plyinfo.Antag;
                _playerList.AddItem($"{oocName} was {icName} playing role of {role}.");
            }

            VBox.AddChild(_playerList);
            OpenCentered();
            MoveToFront();

        }
    }

}
