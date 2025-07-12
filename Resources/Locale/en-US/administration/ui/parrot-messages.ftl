# base ui elements
parrot-messages-title = Parrot messages

# base controls
parrot-messages-loading = Loading parrot messages...
parrot-messages-num-messages = { $messageCount -> 
    [0] No parrot messages in this category.
    [1] 1 parrot message in this category.
    *[other] { $messageCount } parrot messages in this category.
}

parrot-messages-refresh = Refresh

# filter
parrot-messages-text-filter = Text filter
parrot-messages-apply-filter = Apply filter
parrot-messages-clear-filter = Clear filter

# message line elements
parrot-messages-line-ahelp-tooltip = Ahelp this user. If this button is greyed out, this user is not in the current round or online.

parrot-messages-line-block = Block
parrot-messages-line-block-tooltip = Block this message, preventing it from being picked by entities using the parrot message database and preventing it from being learnt.
parrot-messages-line-unblock = Unblock
parrot-messages-line-unblock-tooltip = Unblock this message. If there is a Cvar set to discard old messages, this message may be discarded. Otherwise, this message can again be picked by entities using the parrot message database.
