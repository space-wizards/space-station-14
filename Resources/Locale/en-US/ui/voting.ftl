### Voting menu stuff

# Displayed as initiator of vote when no user creates the vote
ui-vote-initiator-server = The server

ui-vote-restart-title = Restart round
ui-vote-restart-succeeded = Restart vote succeeded.
ui-vote-restart-failed = Restart vote failed (need { TOSTRING($ratio, "P0") }).
ui-vote-restart-yes = Yes
ui-vote-restart-no = No

ui-vote-gamemode-title = Next gamemode
ui-vote-gamemode-tie = Tie for gamemode vote! Picking... { $picked }
ui-vote-gamemode-win = { $winner } won the gamemode vote!

ui-vote-created = { $initiator } has called a vote:
ui-vote-button  = { $text } ({ $votes })

ui-vote-type-restart = Restart round
ui-vote-type-gamemode = Next gamemode

# Window title of the vote create menu
ui-vote-create-title = Call Vote
# Submit button in the vote create button
ui-vote-create-button = Call Vote
# Hue hue hue
ui-vote-fluff = Powered by Robust™ Anti-Tamper Technology

# Button text in lobby/escape menu
ui-vote-menu-button = Call vote

# Vote menu command
ui-vote-menu-command-description = Opens the voting menu
ui-vote-menu-command-help-text = Usage: votemenu

# CreateVoteCommand
create-vote-command-description = Creates a vote
create-vote-command-help = Usage: createvote <'restart'|'preset'>
create-vote-command-cannot-call-vote-now = You can't call a vote right now!
create-vote-command-invalid-vote-type = You can't call a vote right now!

# CreateCustomCommand
create-custom-command-description = Creates a custom vote
create-custom-command-help = customvote <title> <option1> <option2> [option3...]
create-custom-command-on-finished-tie = Tie between {$ties}!
create-custom-command-on-finished-win = {$winner} wins!

# VoteCommand
vote-command-description = Votes on an active vote
vote-command-help = vote <voteId> <option>
vote-command-cannot-call-vote-now = You can't call a vote right now!
vote-command-on-execute-error-must-be-player = Must be a player
vote-command-on-execute-error-invalid-vote-id = Invalid vote ID
vote-command-on-execute-error-invalid-vote-options = Invalid vote options
vote-command-on-execute-error-invalid-vote = Invalid vote
vote-command-on-execute-error-invalid-option = Invalid option

# ListVotesCommand
list-votes-command-description = Lists currently active votes
list-votes-command-help = Usage: listvotes

# CancelVoteCommand
cancel-vote-command-description = Cancels an active vote
cancel-vote-command-help = Usage: cancelvote <id>
                           You can get the ID from the listvotes command.
cancel-vote-command-on-execute-error-invalid-vote-id = Invalid vote ID
cancel-vote-command-on-execute-error-missing-vote-id = Missing ID

# VoteOptions
vote-options-server-initiator-text = The server