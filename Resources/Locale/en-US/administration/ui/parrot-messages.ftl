# base ui elements
parrot-messages-title = Parrot messages

# base controls
parrot-messages-loading = Loading parrot messages...
parrot-messages-num-messages = { $messageCount ->
    [0] No parrot messages
    [1] 1 parrot message
    *[other] { $messageCount } parrot messages
}

parrot-messages-refresh = Refresh

# filter
parrot-messages-text-filter = Text filter
parrot-messages-current-round-filter = Current round only
parrot-messages-apply-filter = Apply filter
parrot-messages-clear-filter = Clear filter

# message line elements
parrot-messages-line-current-round = Learnt this round

parrot-messages-line-ahelp-tooltip = Ahelp this user.

parrot-messages-line-block = Block
parrot-messages-line-block-tooltip = Block this message, preventing it from being picked by entities using the parrot message database and preventing it from being learnt.
parrot-messages-line-unblock = Unblock
parrot-messages-line-unblock-tooltip = Unblock this message. If there is a Cvar set to discard old messages, this message may be discarded. Otherwise, this message can again be picked by entities using the parrot message database.
