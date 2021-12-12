using System;
using System.Linq;
using Content.Client.Message;
using Content.Shared.GameTicking;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.RoundEnd
{
    public sealed class RoundEndSummaryWindow : SS14Window
    {

        public RoundEndSummaryWindow(string gm, string roundEnd, TimeSpan roundTimeSpan, RoundEndMessageEvent.RoundEndPlayerInfo[] info)
        {
            MinSize = SetSize = (520, 580);

            Title = Loc.GetString("round-end-summary-window-title");

            // The round end window is split into two tabs, one about the round stats
            // and the other is a list of RoundEndPlayerInfo for each player.
            // This tab would be a good place for things like: "x many people died.",
            // "clown slipped the crew x times.", "x shots were fired this round.", etc.
            // Also good for serious info.

            var roundEndTabs = new TabContainer();
            roundEndTabs.AddChild(MakeRoundEndSummaryTab(gm, roundEnd, roundTimeSpan));
            roundEndTabs.AddChild(MakePlayerManifestoTab(info));

            Contents.AddChild(roundEndTabs);

            OpenCentered();
            MoveToFront();
        }

        private BoxContainer MakeRoundEndSummaryTab(string gamemode, string roundEnd, TimeSpan roundDuration)
        {
            var roundEndSummaryTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-round-end-summary-tab-title")
            };

            var roundEndSummaryContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true
            };
            var roundEndSummaryContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            //Gamemode Name
            var gamemodeLabel = new RichTextLabel();
            gamemodeLabel.SetMarkup(Loc.GetString("round-end-summary-window-gamemode-name-label", ("gamemode", gamemode)));
            roundEndSummaryContainer.AddChild(gamemodeLabel);

            //Duration
            var roundTimeLabel = new RichTextLabel();
            roundTimeLabel.SetMarkup(Loc.GetString("round-end-summary-window-duration-label",
                                                   ("hours", roundDuration.Hours),
                                                   ("minutes", roundDuration.Minutes),
                                                   ("seconds", roundDuration.Seconds)));
            roundEndSummaryContainer.AddChild(roundTimeLabel);

            //Round end text
            if (!string.IsNullOrEmpty(roundEnd))
            {
                var roundEndLabel = new RichTextLabel();
                roundEndLabel.SetMarkup(roundEnd);
                roundEndSummaryContainer.AddChild(roundEndLabel);
            }

            roundEndSummaryContainerScrollbox.AddChild(roundEndSummaryContainer);
            roundEndSummaryTab.AddChild(roundEndSummaryContainerScrollbox);

            return roundEndSummaryTab;
        }

        private BoxContainer MakePlayerManifestoTab(RoundEndMessageEvent.RoundEndPlayerInfo[] playersInfo)
        {
            var playerManifestTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-player-manifesto-tab-title")
            };

            var playerInfoContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true
            };
            var playerInfoContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            //Put observers at the bottom of the list. Put antags on top.
            var sortedPlayersInfo = playersInfo.OrderBy(p => p.Observer).ThenBy(p => !p.Antag);

            //Create labels for each player info.
            foreach (var playerInfo in sortedPlayersInfo)
            {
                var playerInfoText = new RichTextLabel();

                if (playerInfo.PlayerICName != null)
                {
                    if (playerInfo.Observer)
                    {
                        playerInfoText.SetMarkup(
                            Loc.GetString("round-end-summary-window-player-info-if-observer-text",
                                          ("playerOOCName", playerInfo.PlayerOOCName),
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
                                ("playerICName", playerInfo.PlayerICName),
                                ("playerRole", Loc.GetString(playerInfo.Role))));
                    }
                }
                playerInfoContainer.AddChild(playerInfoText);
            }

            playerInfoContainerScrollbox.AddChild(playerInfoContainer);
            playerManifestTab.AddChild(playerInfoContainerScrollbox);

            return playerManifestTab;
        }
    }

}
