# Displayed as initiator of vote when no user creates the vote
ui-vote-initiator-server = The server

## Default.Votes

ui-vote-restart-title = Restart round
ui-vote-restart-succeeded = Restart vote succeeded.
ui-vote-restart-failed = Restart vote failed (need { TOSTRING($ratio, "P0") }).
ui-vote-restart-fail-not-enough-ghost-players = Restart vote failed: A minimum of { $ghostPlayerRequirement }% ghost players is required to initiate a restart vote. Currently, there are not enough ghost players.
ui-vote-restart-yes = Yes
ui-vote-restart-no = No
ui-vote-restart-abstain = Abstain

ui-vote-gamemode-title = Next gamemode
ui-vote-gamemode-tie = Tie for gamemode vote! Picking... { $picked }
ui-vote-gamemode-win = { $winner } won the gamemode vote!

ui-vote-map-title = Next map
ui-vote-map-tie = Tie for map vote! Picking... { $picked }
ui-vote-map-win = { $winner } won the map vote!
ui-vote-map-notlobby = Voting for maps is only valid in the pre-round lobby!
ui-vote-map-notlobby-time = Voting for maps is only valid in the pre-round lobby with { $time } remaining!
