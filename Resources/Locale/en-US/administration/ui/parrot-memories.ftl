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

parrot-memory-line-block = { $blocked ->
 *[false] Block
 [true] Unblock
}
parrot-memory-line-block-tooltip = { $blocked ->
 *[false] Block this memory, preventing it from being picked by entities using the parrot memory database.
 [true] Unblock this memory, returning it to the pool of potential memories for parrots. If this entry is old, it may be truncated at the start of the next round.
}
