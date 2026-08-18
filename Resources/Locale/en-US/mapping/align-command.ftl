cmd-align-desc =
    Automatically snap the alignment of all anchored airlocks, doors, firelocks etc.
    to line up with adjacent structures.

    Use the [dry run] parameter to perform a check without rotating anything.
cmd-align-help = Usage: {$command} [MapID] [dry run?]
cmd-align-no-release = You can't use this command if the game is running in RELEASE configuration.
cmd-align-hint-id = MapID
cmd-align-hint-dry = dry run?
cmd-align-feedback-none = {$dry ->
[true] DRY RUN: No
*[false] No
} entities compatible with AlignerSystem were found!
cmd-align-feedback-good = {$dry ->
[true] DRY RUN: No
*[false] No
} misaligned entities were found.
cmd-align-feedback = {$dry ->
[true] DRY RUN: Found
*[false] Found and fixed
} {$fixed ->
[one] a single misaligned entity.
*[else] {$fixed} misaligned entities.
}
