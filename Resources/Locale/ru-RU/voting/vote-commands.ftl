## CreateVoteCommand

create-vote-command-description = Creates a vote
create-vote-command-help = Usage: createvote <'restart'|'preset'>
create-vote-command-cannot-call-vote-now = You can't call a vote right now!
create-vote-command-invalid-vote-type = You can't call a vote right now!

## CreateCustomCommand

create-custom-command-description = Creates a custom vote
create-custom-command-help = customvote <title> <option1> <option2> [option3...]
create-custom-command-on-finished-tie = Tie between {$ties}!
create-custom-command-on-finished-win = {$winner} wins!

## VoteCommand

vote-command-description = Votes on an active vote
vote-command-help = vote <voteId> <option>
vote-command-cannot-call-vote-now = You can't call a vote right now!
vote-command-on-execute-error-must-be-player = Must be a player
vote-command-on-execute-error-invalid-vote-id = Invalid vote ID
vote-command-on-execute-error-invalid-vote-options = Invalid vote options
vote-command-on-execute-error-invalid-vote = Invalid vote
vote-command-on-execute-error-invalid-option = Invalid option

## ListVotesCommand

list-votes-command-description = Lists currently active votes
list-votes-command-help = Usage: listvotes

## CancelVoteCommand

cancel-vote-command-description = Cancels an active vote
cancel-vote-command-help = Usage: cancelvote <id>
                           You can get the ID from the listvotes command.
cancel-vote-command-on-execute-error-invalid-vote-id = Invalid vote ID
cancel-vote-command-on-execute-error-missing-vote-id = Missing ID