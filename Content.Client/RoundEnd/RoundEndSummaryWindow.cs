using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Message;
using Content.Shared.GameTicking;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using static Content.Shared.GameTicking.SharedGameTicker;

namespace Content.Client.RoundEnd
{
    public sealed class RoundEndSummaryWindow : SS14Window
    {
        private VBoxContainer RoundEndSummaryTab { get; }
        private VBoxContainer PlayerManifestoTab { get; }
        private TabContainer RoundEndWindowTabs { get; }

        public RoundEndSummaryWindow(string gm, string roundEnd, TimeSpan roundTimeSpan, RoundEndMessageEvent.RoundEndPlayerInfo[] info)
        {
            MinSize = SetSize = (520, 580);

            Title = Loc.GetString("round-end-summary-window-title");

            //Round End Window is split into two tabs, one about the round stats
            //and the other is a list of RoundEndPlayerInfo for each player.
            //This tab would be a good place for things like: "x many people died.",
            //"clown slipped the crew x times.", "x shots were fired this round.", etc.
            //Also good for serious info.
            RoundEndSummaryTab = new VBoxContainer()
            {
                Name = Loc.GetString("round-end-summary-window-round-end-summary-tab-title")
            };

            //Tab for listing  unique info per player.
            PlayerManifestoTab = new VBoxContainer()
            {
                Name = Loc.GetString("round-end-summary-window-player-manifesto-tab-title")
            };

            RoundEndWindowTabs = new TabContainer();
            RoundEndWindowTabs.AddChild(RoundEndSummaryTab);
            RoundEndWindowTabs.AddChild(PlayerManifestoTab);

            Contents.AddChild(RoundEndWindowTabs);

            //Gamemode Name
            var gamemodeLabel = new RichTextLabel();
            gamemodeLabel.SetMarkup(Loc.GetString("round-end-summary-window-gamemode-name-label", ("gamemode",gm)));
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
            roundTimeLabel.SetMarkup(Loc.GetString("round-end-summary-window-duration-label",
                                                   ("hours",roundTimeSpan.Hours),
                                                   ("minutes",roundTimeSpan.Minutes),
                                                   ("seconds",roundTimeSpan.Seconds)));
            RoundEndSummaryTab.AddChild(roundTimeLabel);

            //Initialize what will be the list of players display.
            var scrollContainer = new ScrollContainer
            {
                VerticalExpand = true
            };
            var innerScrollContainer = new VBoxContainer();

            //Put observers at the bottom of the list. Put antags on top.
            var manifestSortedList = info.OrderBy(p => p.Observer).ThenBy(p => !p.Antag);
            //Create labels for each player info.
            foreach (var playerInfo in manifestSortedList)
            {
                var playerInfoText = new RichTextLabel();

                if (playerInfo.PlayerICName != null)
                {
                    if (playerInfo.Observer)
                    {
                        playerInfoText.SetMarkup(
                            Loc.GetString("round-end-summary-window-player-info-if-observer-text",
                                          ("playerOOCName",playerInfo.PlayerOOCName),
                                          ("playerICName", playerInfo.PlayerICName)));
                    }
                    else
                    {
                        //TODO: On Hover display a popup detailing more play info.
                        //For example: their antag goals and if they completed them sucessfully.
                        var icNameColor = playerInfo.Antag ? "red" : "white";
                        playerInfoText.SetMarkup(
                            Loc.GetString("round-end-summary-window-player-info-if-not-observer-text",
                                ("playerOOCName", playerInfo.PlayerOOCName),
                                ("icNameColor", icNameColor),
                                ("playerICName",playerInfo.PlayerICName),
                                ("playerRole", Loc.GetString(playerInfo.Role))));
                    }
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
