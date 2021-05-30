
### UI

# Shown at the end of a round of Traitor
traitor-round-end-result = {$traitorCount ->
    [one] There was one traitor.
    *[other] There were {$traitorCount} traitors.
}

# Shown at the end of a round of Traitor
traitor-user-was-a-traitor = {$user} was a traitor.

# Shown at the end of a round of Traitor
traitor-objective-list-start = and had the following objectives:

# Shown at the end of a round of Traitor
traitor-objective-condition-success = {$condition} | [color={$markupColor}]Success![/color]

# Shown at the end of a round of Traitor
traitor-objective-condition-fail = {$condition} | [color={$markupColor}]Failure![/color] ({$progress}%)