# base ui elements
parrot-memory-title = Parrot memory

# base controls
parrot-memory-loading = Loading parrot memories...
parrot-memory-num-memories = { $memoryCount ->
    [0] No parrot memories
    [1] 1 parrot memory
    *[other] { $memoryCount } parrot memories
}

# general controls
parrot-memory-refresh = Refresh
parrot-memory-go-to-round = Go
parrot-memory-to-current-round = To current round

# filter
parrot-memory-text-filter = Text filter
parrot-memory-apply-filter = Apply filter
parrot-memory-clear-filter = Clear filter

# memory line elements
parrot-memory-line-current-round = Learnt this round

parrot-memory-line-ahelp-tooltip = Ahelp this user.

parrot-memory-line-block = Block
parrot-memory-line-block-tooltip = Block this memory, preventing it from being picked by entities using the parrot memory database and preventing it from being learnt.
parrot-memory-line-unblock = Unblock
parrot-memory-line-unblock-tooltip = Unblock this memory. If there is a Cvar set to discard old memories, this memory may be discarded. Otherwise, this memory can again be picked by entities using the parrot memory database.
