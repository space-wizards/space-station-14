using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Utility;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using static Content.Shared.GameTicking.SharedGameTicker;

namespace Content.Client.UserInterface
{
    public sealed class RoundEndSummaryWindow : SS14Window
    {
        private VBoxContainer RoundEndSummaryTab { get; }
        private VBoxContainer PlayerManifestoTab { get; }
        private TabContainer RoundEndWindowTabs { get; }

        public RoundEndSummaryWindow(string gm, string roundEnd, TimeSpan roundTimeSpan, List<RoundEndPlayerInfo> info)
        {
            MinSize = SetSize = (520, 580);

            Title = Loc.GetString("Round End Summary");

            //Round End Window is split into two tabs, one about the round stats
            //and the other is a list of RoundEndPlayerInfo for each player.
            //This tab would be a good place for things like: "x many people died.",
            //"clown slipped the crew x times.", "x shots were fired this round.", etc.
            //Also good for serious info.
            RoundEndSummaryTab = new VBoxContainer()
            {
                Name = Loc.GetString("Round Information")
            };

            //Tab for listing  unique info per player.
            PlayerManifestoTab = new VBoxContainer()
            {
                Name = Loc.GetString("Player Manifesto")
            };

            RoundEndWindowTabs = new TabContainer();
            RoundEndWindowTabs.AddChild(RoundEndSummaryTab);
            RoundEndWindowTabs.AddChild(PlayerManifestoTab);

            Contents.AddChild(RoundEndWindowTabs);

            //Gamemode Name
            var gamemodeLabel = new RichTextLabel();
            gamemodeLabel.SetMarkup(Loc.GetString("Round of [color=white]{0}[/color] has ended.", gm));
            RoundEndSummaryTab.AddChild(gamemodeLabel);

            //Round end text
            if (!string.IsNullOrEmpty(roundEnd))
            {
                var roundEndLabel = new RichTextLabel();
                roundEndLabel.SetMarkup(Loc.GetString(roundEnd));
                RoundEndSummaryTab.AddChild(roundEndLabel);
            }

            //Duration
            var roundTimeLabel = new RichTextLabel();
            roundTimeLabel.SetMarkup(Loc.GetString("It lasted for [color=yellow]{0} hours, {1} minutes, and {2} seconds.",
                roundTimeSpan.Hours,roundTimeSpan.Minutes,roundTimeSpan.Seconds));
            RoundEndSummaryTab.AddChild(roundTimeLabel);

            //Initialize what will be the list of players display.
            var scrollContainer = new ScrollContainer();
            scrollContainer.VerticalExpand = true;
            var innerScrollContainer = new VBoxContainer();

            //Put observers at the bottom of the list. Put antags on top.
            var manifestSortedList = info.OrderBy(p => p.Observer).ThenBy(p => !p.Antag);
            //Create labels for each player info.
            foreach (var playerInfo in manifestSortedList)
            {
                var playerInfoText = new RichTextLabel();

                if (playerInfo.Observer)
                {
                    playerInfoText.SetMarkup(
                        Loc.GetString("[color=gray]{0}[/color] was [color=lightblue]{1}[/color], an observer.",
                                        playerInfo.PlayerOOCName, playerInfo.PlayerICName));
                }
                else
                {
                    //TODO: On Hover display a popup detailing more play info.
                    //For example: their antag goals and if they completed them sucessfully.
                    var icNameColor = playerInfo.Antag ? "red" : "white";
                    playerInfoText.SetMarkup(
                        Loc.GetString("[color=gray]{0}[/color] was [color={1}]{2}[/color] playing role of [color=orange]{3}[/color].",
                                        playerInfo.PlayerOOCName, icNameColor, playerInfo.PlayerICName, Loc.GetString(playerInfo.Role)));
                }
                innerScrollContainer.AddChild(playerInfoText);
            }

            scrollContainer.AddChild(innerScrollContainer);
            //Attach the entire ScrollContainer that holds all the playerinfo.
            PlayerManifestoTab.AddChild(scrollContainer);

            //Finally, display the window.
            OpenCentered();
            MoveToFront();
        }
    }

}
