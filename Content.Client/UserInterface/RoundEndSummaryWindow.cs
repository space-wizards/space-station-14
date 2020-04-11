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
        private VBoxContainer VBox { get; }
        protected override Vector2? CustomSize => (520, 580);

        public RoundEndSummaryWindow(string gm, uint duration, List<RoundEndPlayerInfo> info )
        {
            Title = Loc.GetString("Round End Summary");
            VBox = new VBoxContainer();
            Contents.AddChild(VBox);
            //Gamemode Name
            var gamemodeLabel = new RichTextLabel();
            gamemodeLabel.SetMarkup(Loc.GetString("Round of [color=white]{0}[/color] has ended.", gm));
            VBox.AddChild(gamemodeLabel);

            //Duration
            //var roundDurationInfo = new RichTextLabel();
            //roundDurationInfo.SetMarkup(Loc.GetString("The round lasted for [color=yellow]{0}[/color] hours.", duration));
            //VBox.AddChild(roundDurationInfo);


            //Initialize what will be the list of players display.
            var scrollContainer = new ScrollContainer();
            scrollContainer.SizeFlagsVertical = SizeFlags.FillExpand;
            var innerScrollContainer = new VBoxContainer();

            //Create labels for each player info.
            foreach (var plyinfo in info)
            {
                var oocName = plyinfo.PlayerOOCName;
                var icName = plyinfo.PlayerICName;
                var role = plyinfo.Role;
                var wasAntag = plyinfo.Antag;

                var playerInfoText = new RichTextLabel();
                playerInfoText.SetMarkup(Loc.GetString($"[color=gray]{oocName}[/color] was [color=white]{icName}[/color] playing role of [color=orange]{role}[/color]."));
                innerScrollContainer.AddChild(playerInfoText);
            }

            scrollContainer.AddChild(innerScrollContainer);
            //Attach the entire ScrollContainer that holds all the playerinfo.
            VBox.AddChild(scrollContainer);

            //Finally, display the window.
            OpenCentered();
            MoveToFront();

        }
    }

}
